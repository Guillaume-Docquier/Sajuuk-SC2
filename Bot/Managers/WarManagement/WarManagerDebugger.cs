using System.Collections.Generic;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;

namespace Bot.Managers.WarManagement;

public class WarManagerDebugger {
    public void Debug(HashSet<Unit> army) {
        foreach (var soldier in army) {
            Program.GraphicalDebugger.AddText("W", worldPos: soldier.Position.ToPoint(), color: Colors.Red);
        }
    }
}
