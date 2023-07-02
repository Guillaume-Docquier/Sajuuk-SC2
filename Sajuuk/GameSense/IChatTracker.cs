using System.Collections.Generic;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public interface IChatTracker {
    public List<ChatReceived> NewBotChat { get; }
    public List<ChatReceived> NewEnemyChat { get; }
}
