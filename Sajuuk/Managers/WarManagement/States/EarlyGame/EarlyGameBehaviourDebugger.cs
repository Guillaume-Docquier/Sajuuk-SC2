using System.Collections.Generic;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Debugging;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.Managers.WarManagement.States.MidGame;
using Sajuuk.MapAnalysis.RegionAnalysis;
using SC2APIProtocol;

namespace Sajuuk.Managers.WarManagement.States.EarlyGame;

public class EarlyGameBehaviourDebugger {
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }
    public Stance CurrentStance { get; set; }
    public IRegion Target { get; set; }

    public BuildRequestPriority BuildPriority { get; set; }
    public BuildBlockCondition BuildBlockCondition { get; set; }

    public EarlyGameBehaviourDebugger(IDebuggingFlagsTracker debuggingFlagsTracker, IGraphicalDebugger graphicalDebugger) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _graphicalDebugger = graphicalDebugger;
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

        _graphicalDebugger.AddTextGroup(texts, virtualPos: new Point { X = 0.30f, Y = 0.02f });
    }
}
