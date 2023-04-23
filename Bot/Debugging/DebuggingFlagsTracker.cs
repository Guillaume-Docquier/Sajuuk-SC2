using System.Collections.Generic;
using System.Linq;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.Debugging;

public class DebuggingFlagsTracker : IDebuggingFlagsTracker, INeedUpdating {
    /// <summary>
    /// DI: ✔️ The only usages are for static instance creations
    /// </summary>
    public static readonly DebuggingFlagsTracker Instance = new DebuggingFlagsTracker(ChatTracker.Instance);

    private readonly IChatTracker _chatTracker;

    private readonly HashSet<string> _activeDebuggingFlags = new HashSet<string>();

    private IReadOnlySet<string> AllDebuggingFlags { get; } = DebuggingFlags.GetAll();
    private IReadOnlySet<string> AllDebuggingCommands { get; } = DebuggingCommands.GetAll();

    private DebuggingFlagsTracker(IChatTracker chatTracker) {
        _chatTracker = chatTracker;

        if (Program.DebugEnabled) {
            Reset();
        }
    }

    public bool IsActive(string debuggingFlag) {
        return _activeDebuggingFlags.Contains(debuggingFlag);
    }

    public void Reset() {
        _activeDebuggingFlags.Clear();

        _activeDebuggingFlags.Add(DebuggingFlags.Help);
        _activeDebuggingFlags.Add(DebuggingFlags.BuildOrder);
        _activeDebuggingFlags.Add(DebuggingFlags.MatchupData);
        _activeDebuggingFlags.Add(DebuggingFlags.IncomeRate);
        _activeDebuggingFlags.Add(DebuggingFlags.Spend);
        _activeDebuggingFlags.Add(DebuggingFlags.EnemyDetectors);
        _activeDebuggingFlags.Add(DebuggingFlags.GhostUnits);
        _activeDebuggingFlags.Add(DebuggingFlags.KnownEnemyUnits);
        _activeDebuggingFlags.Add(DebuggingFlags.Regions);
        _activeDebuggingFlags.Add(DebuggingFlags.WarManager);
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (!Program.DebugEnabled) {
            return;
        }

        var messages = _chatTracker.NewBotChat.SelectMany(chatReceived => chatReceived.Message.Split(" "));
        foreach (var message in messages) {
            HandleMessage(message);
        }
    }

    public void HandleMessage(string message) {
        if (AllDebuggingCommands.Contains(message)) {
            HandleCommand(message);
        }
        else if (AllDebuggingFlags.Contains(message)) {
            ToggleFlag(message);
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
