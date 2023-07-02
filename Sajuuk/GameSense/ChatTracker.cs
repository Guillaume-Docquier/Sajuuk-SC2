using System.Collections.Generic;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public class ChatTracker : IChatTracker, INeedUpdating {
    public List<ChatReceived> NewBotChat { get; } = new List<ChatReceived>();
    public List<ChatReceived> NewEnemyChat { get; } = new List<ChatReceived>();

    public ChatTracker() {}

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
