using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.GameSense;

public class ChatTracker : IChatTracker, INeedUpdating {
    // DI: ✔️ The only usages are for static instance creations
    public static readonly ChatTracker Instance = new ChatTracker();

    public List<ChatReceived> NewBotChat { get; } = new List<ChatReceived>();
    public List<ChatReceived> NewEnemyChat { get; } = new List<ChatReceived>();

    private ChatTracker() {}

    public void Reset() {
        NewBotChat.Clear();
        NewEnemyChat.Clear();
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        Reset();

        var playerId = observation.Observation.PlayerCommon.PlayerId;
        foreach (var chat in observation.Chat) {
            if (chat.PlayerId == playerId) {
                NewBotChat.Add(chat);
            }
            else {
                NewEnemyChat.Add(chat);
            }
        }
    }
}
