using System.Collections.Generic;
using Bot.Builds;
using Bot.Debugging;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameBehaviourDebugger {
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;

    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }

    public BuildRequestPriority BuildPriority { get; set; }
    public BuildBlockCondition BuildBlockCondition { get; set; }

    public MidGameBehaviourDebugger(IDebuggingFlagsTracker debuggingFlagsTracker) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
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

        Program.GraphicalDebugger.AddTextGroup(texts, virtualPos: new Point { X = 0.30f, Y = 0.02f });
    }
}
