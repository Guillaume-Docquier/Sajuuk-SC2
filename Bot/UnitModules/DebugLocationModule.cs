using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using SC2APIProtocol;

namespace Bot.UnitModules;

public class DebugLocationModule: UnitModule {
    public const string Tag = "DebugLocationModule";

    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly Unit _unit;
    private readonly Color _color;
    private readonly bool _showName;

    public static void Install(IGraphicalDebugger graphicalDebugger, Unit unit, Color color = null, bool showName = false) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new DebugLocationModule(graphicalDebugger, unit, color, showName));
        }
    }

    private DebugLocationModule(IGraphicalDebugger graphicalDebugger, Unit unit, Color color = null, bool showName = false) {
        _graphicalDebugger = graphicalDebugger;
        _unit = unit;
        _color = color ?? Colors.White;
        _showName = showName;
    }

    protected override void DoExecute() {
        _graphicalDebugger.AddSphere(_unit, _color);
        if (_showName) {
            _graphicalDebugger.AddText($"{_unit.Name} [{_unit.UnitType}]", worldPos: _unit.Position.ToPoint());
        }
    }
}
