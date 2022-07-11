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

    public UnitsTracker(IEnumerable<SC2APIProtocol.Unit> rawUnits, ulong frame) {
        var units = rawUnits.Select(rawUnit => new Unit(rawUnit, frame)).ToList();

        _unitsMap = units.ToDictionary(unit => unit.Tag);

        NeutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        OwnedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        EnemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();
    }

    public void Update(List<SC2APIProtocol.Unit> newRawUnits, ulong frame) {
        NewOwnedUnits.Clear();
        DeadOwnedUnits.Clear();

        // Find new units and update existing ones
        newRawUnits.ForEach(newRawUnit => {
            // Existing unit, update it
            if (_unitsMap.ContainsKey(newRawUnit.Tag)) {
                _unitsMap[newRawUnit.Tag].Update(newRawUnit, frame);
            }
            else {
                var newUnit = new Unit(newRawUnit, frame);

                // New owned unit
                if (newUnit.Alliance == Alliance.Self) {
                    NewOwnedUnits.Add(newUnit);
                }
                else if (newUnit.Alliance == Alliance.Neutral) {
                    var equivalentUnit = _unitsMap
                        .Select(kv => kv.Value)
                        .FirstOrDefault(unit => unit.Position == newUnit.Position);

                    // Resources have 2 units representing them
                    // The snapshot version and the real version
                    // The real version is only available when visible
                    // The snapshot is only available when not visible
                    if (equivalentUnit != default) {
                        _unitsMap.Remove(equivalentUnit.Tag);
                        equivalentUnit.Update(newRawUnit, frame);
                        newUnit = equivalentUnit;
                    }
                    else {
                        Logger.Warning("New neutral unit: {0}", newUnit.Name);
                    }
                }

                // New unit
                _unitsMap[newUnit.Tag] = newUnit;
            }
        });

        // Find dead units
        foreach (var unit in _unitsMap.Select(unit => unit.Value).ToList()) {
            if (unit.IsDead(frame)) {
                // Dead owned unit
                if (unit.Alliance == Alliance.Self) {
                    DeadOwnedUnits.Add(unit);
                }
                else if (unit.Alliance == Alliance.Neutral) {
                    // TODO GD Handle neutral units dying
                    Logger.Warning("Neutral unit died: {0}", unit.Name);
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
