using System.Collections.Generic;
using System.Linq;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.Debugging;

public class DebuggingFlagsTracker : INeedUpdating {
    public static readonly DebuggingFlagsTracker Instance = new DebuggingFlagsTracker();

    private readonly HashSet<string> _activeDebuggingFlags = new HashSet<string>();

    public static IReadOnlySet<string> AllDebuggingFlags { get; } = DebuggingFlags.GetAll();
    public static IReadOnlySet<string> ActiveDebuggingFlags => Instance._activeDebuggingFlags;

    private DebuggingFlagsTracker() {
        Reset();
    }

    public void Reset() {
        _activeDebuggingFlags.Clear();

        _activeDebuggingFlags.Add(DebuggingFlags.Help);
        _activeDebuggingFlags.Add(DebuggingFlags.BuildOrder);
        _activeDebuggingFlags.Add(DebuggingFlags.MatchupData);
        _activeDebuggingFlags.Add(DebuggingFlags.IncomeRate);
        _activeDebuggingFlags.Add(DebuggingFlags.EnemyDetectors);
        _activeDebuggingFlags.Add(DebuggingFlags.GhostUnits);
        _activeDebuggingFlags.Add(DebuggingFlags.KnownEnemyUnits);
        _activeDebuggingFlags.Add(DebuggingFlags.Regions);
    }

    public void Update(ResponseObservation observation) {
        if (!Program.DebugEnabled) {
            return;
        }

        var debugCommands = ChatTracker.NewBotChat
            .SelectMany(chatReceived => chatReceived.Message.Split(" "))
            .Where(word => AllDebuggingFlags.Contains(word));

        foreach (var debugCommand in debugCommands) {
            if (debugCommand is DebuggingFlags.Reset) {
                Reset();
            }
            else if (_activeDebuggingFlags.Contains(debugCommand)) {
                _activeDebuggingFlags.Remove(debugCommand);
            }
            else {
                _activeDebuggingFlags.Add(debugCommand);
            }
        }
    }
}
