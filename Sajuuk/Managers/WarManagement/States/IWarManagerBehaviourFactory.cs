using Sajuuk.Managers.WarManagement.States.EarlyGame;
using Sajuuk.Managers.WarManagement.States.Finisher;
using Sajuuk.Managers.WarManagement.States.MidGame;

namespace Sajuuk.Managers.WarManagement.States;

public interface IWarManagerBehaviourFactory {
    public EarlyGameBehaviour CreateEarlyGameBehaviour(WarManager warManager);
    public MidGameBehaviour CreateMidGameBehaviour(WarManager warManager);
    public FinisherBehaviour CreateFinisherBehaviour(WarManager warManager);
}
