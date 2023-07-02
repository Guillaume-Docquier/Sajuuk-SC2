using System.Collections.Generic;
using Sajuuk.Debugging;
using Sajuuk.Debugging.GraphicalDebugging;
using SC2APIProtocol;

namespace Sajuuk.Managers.WarManagement.States.Finisher;

public class FinisherBehaviourDebugger {
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }

    public FinisherBehaviourDebugger(IDebuggingFlagsTracker debuggingFlagsTracker, IGraphicalDebugger graphicalDebugger) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _graphicalDebugger = graphicalDebugger;
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

        _graphicalDebugger.AddTextGroup(texts, virtualPos: new Point { X = 0.30f, Y = 0.02f });
    }
}
