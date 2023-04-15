using System.Collections.Generic;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;

namespace Bot.Managers.WarManagement;

public class WarManagerDebugger {
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

            Program.GraphicalDebugger.AddText(text, worldPos: soldier.Position.ToPoint(yOffset: 0.17f), color: Colors.Red);
        }
    }
}
