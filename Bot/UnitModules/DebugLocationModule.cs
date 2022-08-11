﻿using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.UnitModules;

public class DebugLocationModule: IUnitModule {
    public const string Tag = "debug-location-module";

    private readonly Unit _unit;
    private readonly Color _color;
    private readonly bool _showName;

    public static void Install(Unit unit, Color color = null, bool showName = false) {
        if (UnitModule.PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new DebugLocationModule(unit, color, showName));
        }
    }

    private DebugLocationModule(Unit unit, Color color = null, bool showName = false) {
        _unit = unit;
        _color = color ?? Colors.White;
        _showName = showName;
    }

    public void Execute() {
        GraphicalDebugger.AddSphere(_unit, _color);
        if (_showName) {
            GraphicalDebugger.AddText($"{_unit.Name} [{_unit.UnitType}]", worldPos: _unit.Position.ToPoint());
        }
    }
}
