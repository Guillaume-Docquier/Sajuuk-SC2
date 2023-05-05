using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.Managers.WarManagement.States.MidGame;

namespace Bot.Managers.WarManagement.States;

public interface IWarManagerBehaviourFactory {
    public EarlyGameBehaviour CreateEarlyGameBehaviour(WarManager warManager);
    public MidGameBehaviour CreateMidGameBehaviour(WarManager warManager);
    public FinisherBehaviour CreateFinisherBehaviour(WarManager warManager);
}
