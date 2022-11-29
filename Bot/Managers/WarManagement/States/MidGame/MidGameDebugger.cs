using System.Collections.Generic;
using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.Managers.WarManagement.States.MidGame;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Debugging;

public class WarManagerDebugger {
    public float OwnForce { get; set; }
    public float EnemyForce { get; set; }
    public Stance CurrentStance { get; set; }
    public Region Target { get; set; }

    public BuildRequestPriority BuildPriority { get; set; }
    public BuildBlockCondition BuildBlockCondition { get; set; }

    public void Debug(HashSet<Unit> army) {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.WarManager)) {
            return;
        }

        var texts = new List<string>
        {
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

        foreach (var soldier in army) {
            Program.GraphicalDebugger.AddText("W", worldPos: soldier.Position.ToPoint(), color: Colors.Red);
        }
    }
}
