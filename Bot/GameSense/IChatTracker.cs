using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.GameSense;

public interface IChatTracker {
    public List<ChatReceived> NewBotChat { get; }
    public List<ChatReceived> NewEnemyChat { get; }
}
