using System.Collections.Generic;
using System.Linq;
using SC2APIProtocol;

namespace Bot;

public class UnitsTracker {
    private readonly Dictionary<ulong, Unit> _unitsMap;

    public readonly List<Unit> NeutralUnits;
    public readonly List<Unit> NewOwnedUnits = new List<Unit>();
    public readonly List<Unit> DeadOwnedUnits = new List<Unit>();

    public List<Unit> OwnedUnits;
    public List<Unit> EnemyUnits;

    public UnitsTracker(IEnumerable<SC2APIProtocol.Unit> rawUnits) {
        var units = rawUnits.Select(rawUnit => new Unit(rawUnit)).ToList();

        _unitsMap = units.ToDictionary(unit => unit.Tag);

        NeutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        OwnedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        EnemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();
    }

    public void Update(List<SC2APIProtocol.Unit> newRawUnits) {
        NewOwnedUnits.Clear();
        DeadOwnedUnits.Clear();

        // Find new units and update existing ones
        newRawUnits.ForEach(newRawUnit => {
            // Existing unit, update it
            if (_unitsMap.ContainsKey(newRawUnit.Tag)) {
                _unitsMap[newRawUnit.Tag].Update(newRawUnit);
            }
            else {
                var newUnit = new Unit(newRawUnit);

                // New owned unit
                if (newUnit.Alliance == Alliance.Self) {
                    NewOwnedUnits.Add(newUnit);
                }

                // New unit
                _unitsMap[newUnit.Tag] = newUnit;
            }
        });

        // TODO GD Minerals and gasses can also 'die' (deplete)
        // Find dead units
        var newUnitsDict = newRawUnits.ToDictionary(unit => unit.Tag);
        foreach (var unit in _unitsMap.Select(unit => unit.Value).ToList()) {
            // Cannot find this unit anymore? Probably dead
            if (!newUnitsDict.ContainsKey(unit.Tag)) {
                // Dead owned unit
                if (unit.Alliance == Alliance.Self) {
                    DeadOwnedUnits.Add(unit);
                }

                // Dead unit
                _unitsMap.Remove(unit.Tag);
            }
        }

        // Update unit lists
        OwnedUnits = _unitsMap.Where(unit => unit.Value.Alliance == Alliance.Self).Select(unit => unit.Value).ToList();
        EnemyUnits = _unitsMap.Where(unit => unit.Value.Alliance == Alliance.Enemy).Select(unit => unit.Value).ToList();
    }
}
