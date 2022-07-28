﻿using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using SC2APIProtocol;

namespace Bot;

public class UnitsTracker {
    public Dictionary<ulong, Unit> UnitsByTag;

    public readonly List<Unit> NewOwnedUnits = new List<Unit>();
    public readonly List<Unit> DeadOwnedUnits = new List<Unit>();

    public List<Unit> NeutralUnits;
    public List<Unit> OwnedUnits;
    public List<Unit> EnemyUnits;

    private bool _isInitialized = false;

    public void Update(List<SC2APIProtocol.Unit> newRawUnits, ulong frame) {
        if (!_isInitialized) {
            Init(newRawUnits, frame);

            var unknownNeutralUnits = NeutralUnits.DistinctBy(unit => unit.UnitType)
                .Where(unit => !Units.Destructibles.Contains(unit.UnitType) && !Units.MineralFields.Contains(unit.UnitType) && !Units.GasGeysers.Contains(unit.UnitType))
                .Select(unit => (unit.Name, unit.UnitType))
                .ToList();

            Logger.Info("Unknown neutral units ({0}): {1}", unknownNeutralUnits.Count, string.Join(", ", unknownNeutralUnits));

            return;
        }

        NewOwnedUnits.Clear();
        DeadOwnedUnits.Clear();

        // Find new units and update existing ones
        newRawUnits.ForEach(newRawUnit => {
            if (UnitsByTag.ContainsKey(newRawUnit.Tag)) {
                UnitsByTag[newRawUnit.Tag].Update(newRawUnit, frame);
            }
            else {
                var newUnit = new Unit(newRawUnit, frame);

                if (newUnit.Alliance == Alliance.Self) {
                    NewOwnedUnits.Add(newUnit);
                }
                else if (newUnit.Alliance == Alliance.Neutral) {
                    var equivalentUnit = UnitsByTag
                        .Select(kv => kv.Value)
                        .FirstOrDefault(unit => unit.Position == newUnit.Position);

                    // Resources have 2 units representing them: the snapshot version and the real version
                    // The real version is only available when visible
                    // The snapshot is only available when not visible
                    if (equivalentUnit != default) {
                        UnitsByTag.Remove(equivalentUnit.Tag);
                        equivalentUnit.Update(newRawUnit, frame);
                        newUnit = equivalentUnit;
                    }
                    else {
                        Logger.Warning("New neutral unit: {0}", newUnit.Name);
                    }
                }

                UnitsByTag[newUnit.Tag] = newUnit;
            }
        });

        // Handle dead units
        foreach (var unit in UnitsByTag.Select(unit => unit.Value).ToList()) {
            if (unit.IsDead(frame)) {
                if (unit.Alliance == Alliance.Self) {
                    DeadOwnedUnits.Add(unit);
                    unit.Died();
                }
                else if (unit.Alliance == Alliance.Neutral) {
                    unit.Died();
                }

                UnitsByTag.Remove(unit.Tag);
            }
        }

        // Update unit lists
        OwnedUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Self).Select(unit => unit.Value).ToList();
        NeutralUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Neutral).Select(unit => unit.Value).ToList();
        EnemyUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Enemy).Select(unit => unit.Value).ToList();
    }

    private void Init(IEnumerable<SC2APIProtocol.Unit> rawUnits, ulong frame) {
        var units = rawUnits.Select(rawUnit => new Unit(rawUnit, frame)).ToList();

        UnitsByTag = units.ToDictionary(unit => unit.Tag);

        OwnedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        NeutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        EnemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();

        _isInitialized = true;
    }
}
