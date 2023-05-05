using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.Managers.WarManagement.States.MidGame;

namespace Bot.Managers.WarManagement.States;

public interface IWarManagerStateFactory {
    public EarlyGameState CreateEarlyGameState();
    public MidGameState CreateMidGameState();
    public FinisherState CreateFinisherState();
}
