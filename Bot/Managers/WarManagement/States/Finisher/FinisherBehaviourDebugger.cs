using System.Collections.Generic;
using Bot.Debugging;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherBehaviourDebugger {
    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }

    public void Debug() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.WarManager)) {
            return;
        }

        var texts = new List<string>
        {
            "Finisher Behaviour",
            $"Own force:   {OwnForce:F1}",
            $"Enemy force: {EnemyForce:F1}",
        };

        Program.GraphicalDebugger.AddTextGroup(texts, virtualPos: new Point { X = 0.30f, Y = 0.02f });
    }
}
