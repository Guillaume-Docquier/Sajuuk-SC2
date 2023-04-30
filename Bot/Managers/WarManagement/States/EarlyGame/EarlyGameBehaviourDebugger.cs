using System.Collections.Generic;
using Bot.Builds;
using Bot.Debugging;
using Bot.Managers.WarManagement.States.MidGame;
using Bot.MapAnalysis.RegionAnalysis;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameBehaviourDebugger {
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;

    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }
    public Stance CurrentStance { get; set; }
    public IRegion Target { get; set; }

    public BuildRequestPriority BuildPriority { get; set; }
    public BuildBlockCondition BuildBlockCondition { get; set; }

    public EarlyGameBehaviourDebugger(IDebuggingFlagsTracker debuggingFlagsTracker) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
    }

    public void Debug() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.WarManager)) {
            return;
        }

        var texts = new List<string>
        {
            "Early Game Behaviour",
            $"Own force:   {OwnForce:F1}",
            $"Enemy force: {EnemyForce:F1}",
        };

        if (Target != null) {
            texts.Add($"Stance: {CurrentStance} region {Target.Id}");
        }

        texts.Add($"Build");
        texts.Add($" - Priority: {BuildPriority}");
        texts.Add($" - Blocking: {BuildBlockCondition}");

        Program.GraphicalDebugger.AddTextGroup(texts, virtualPos: new Point { X = 0.30f, Y = 0.02f });
    }
}
