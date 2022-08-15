﻿using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.UnitModules;

public class ChangelingTargetingModule: IUnitModule {
    public const string Tag = "ChangelingTargetingModule";

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (UnitModule.PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new ChangelingTargetingModule(unit));
        }
    }

    private ChangelingTargetingModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        var changelings = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Changelings).ToList();
        if (changelings.Count <= 0) {
            return;
        }

        var closestChangelingInRange = changelings.FirstOrDefault(changeling => changeling.HorizontalDistanceTo(_unit) <= _unit.MaxRange);
        if (closestChangelingInRange != null) {
            _unit.Attack(closestChangelingInRange);
        }
    }
}