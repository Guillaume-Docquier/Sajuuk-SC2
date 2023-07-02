using Sajuuk.Managers.WarManagement.States.EarlyGame;
using Sajuuk.Managers.WarManagement.States.Finisher;
using Sajuuk.Managers.WarManagement.States.MidGame;

namespace Sajuuk.Managers.WarManagement.States;

public interface IWarManagerStateFactory {
    public EarlyGameState CreateEarlyGameState();
    public MidGameState CreateMidGameState();
    public FinisherState CreateFinisherState();
}
