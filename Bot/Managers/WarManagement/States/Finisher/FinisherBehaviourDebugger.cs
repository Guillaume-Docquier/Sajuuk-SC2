using System.Collections.Generic;
using Bot.Debugging;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherBehaviourDebugger {
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;

    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }

    public FinisherBehaviourDebugger(IDebuggingFlagsTracker debuggingFlagsTracker) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
    }

    public void Debug() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.WarManager)) {
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
