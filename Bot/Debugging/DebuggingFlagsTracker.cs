using System.Collections.Generic;
using System.Linq;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.Debugging;

public class DebuggingFlagsTracker : INeedUpdating {
    public static readonly DebuggingFlagsTracker Instance = new DebuggingFlagsTracker();

    private readonly HashSet<string> _activeDebuggingFlags = new HashSet<string>();

    public static IReadOnlySet<string> AllDebuggingFlags { get; } = DebuggingFlags.GetAll();
    public static IReadOnlySet<string> AllDebuggingCommands { get; } = DebuggingCommands.GetAll();

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
        //_activeDebuggingFlags.Add(DebuggingFlags.Regions);
        _activeDebuggingFlags.Add(DebuggingFlags.RegionCells);
        _activeDebuggingFlags.Add(DebuggingFlags.ChokePoints);
    }

    public void Update(ResponseObservation observation) {
        if (!Program.DebugEnabled) {
            return;
        }

        var messages = ChatTracker.NewBotChat.SelectMany(chatReceived => chatReceived.Message.Split(" "));
        foreach (var message in messages) {
            if (AllDebuggingCommands.Contains(message)) {
                HandleCommand(message);
            }
            else if (AllDebuggingFlags.Contains(message)) {
                ToggleFlag(message);
            }
        }
    }

    private void HandleCommand(string debugCommand) {
        switch (debugCommand) {
            case DebuggingCommands.Reset:
                Reset();
                break;
            case DebuggingCommands.Off:
                _activeDebuggingFlags.Clear();
                break;
        }
    }

    private void ToggleFlag(string debugFlag) {
        if (_activeDebuggingFlags.Contains(debugFlag)) {
            _activeDebuggingFlags.Remove(debugFlag);
        }
        else {
            _activeDebuggingFlags.Add(debugFlag);
        }
    }
}
