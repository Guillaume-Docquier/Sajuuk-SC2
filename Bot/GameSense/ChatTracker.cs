using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.GameSense;

public class ChatTracker : INeedUpdating {
    public static readonly ChatTracker Instance = new ChatTracker();

    public static readonly List<ChatReceived> NewBotChat = new List<ChatReceived>();
    public static readonly List<ChatReceived> NewEnemyChat = new List<ChatReceived>();

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
