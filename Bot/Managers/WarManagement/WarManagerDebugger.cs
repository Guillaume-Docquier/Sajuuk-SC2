using System.Collections.Generic;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;

namespace Bot.Managers.WarManagement;

public class WarManagerDebugger {
    private readonly IGraphicalDebugger _graphicalDebugger;

    public WarManagerDebugger(IGraphicalDebugger graphicalDebugger) {
        _graphicalDebugger = graphicalDebugger;
    }

    public void Debug(HashSet<Unit> army) {
        foreach (var soldier in army) {
            var text = "";
            if (soldier.WeaponCooldownPercent > 0) {
                text += ".";
            }
            else {
                text += "W";
            }

            if (soldier.RawUnitData.HasEngagedTargetTag) {
                text += "!";
            }

            _graphicalDebugger.AddText(text, worldPos: soldier.Position.ToPoint(yOffset: 0.17f), color: Colors.Red);
        }
    }
}
