using System.Collections.Generic;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Debugging;
using Sajuuk.Debugging.GraphicalDebugging;
using SC2APIProtocol;

namespace Sajuuk.Managers.WarManagement.States.MidGame;

public class MidGameBehaviourDebugger {
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }

    public BuildRequestPriority BuildPriority { get; set; }
    public BuildBlockCondition BuildBlockCondition { get; set; }

    public MidGameBehaviourDebugger(IDebuggingFlagsTracker debuggingFlagsTracker, IGraphicalDebugger graphicalDebugger) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _graphicalDebugger = graphicalDebugger;
    }

    public void Debug() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.WarManager)) {
            return;
        }

        var texts = new List<string>
        {
            "Mid Game Behaviour",
            $"Own force:   {OwnForce:F1}",
            $"Enemy force: {EnemyForce:F1}",
            "Build",
            $" - Priority: {BuildPriority}",
            $" - Blocking: {BuildBlockCondition}",
        };

        _graphicalDebugger.AddTextGroup(texts, virtualPos: new Point { X = 0.30f, Y = 0.02f });
    }
}
