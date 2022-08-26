using System.Linq;
using Bot.UnitModules;

namespace Bot.Managers;

public partial class TownHallManager {
    public void AssignQueen(Unit queen) {
        queen.Supervisor = this;
        queen.AddDeathWatcher(this);

        Queen = queen;

        DebugLocationModule.Install(queen, _color);
        QueenMicroModule.Install(queen, TownHall);
    }

    public void AssignWorker(Unit worker) {
        worker.Supervisor = this;
        worker.AddDeathWatcher(this);

        _workers.Add(worker);

        DebugLocationModule.Install(worker, _color);
        MiningModule.Install(worker, null);

        // TODO GD Do not dispatch inside assign. Dispatch later
        DispatchWorker(worker);
    }

    private void AssignMineral(Unit mineral) {
        mineral.Supervisor = this;
        mineral.AddDeathWatcher(this);

        _minerals.Add(mineral);

        DebugLocationModule.Install(mineral, _color);
        CapacityModule.Install(mineral, MaxPerMinerals);
    }

    private void AssignGas(Unit gas) {
        gas.Supervisor = this;

        _gasses.Add(gas);

        DebugLocationModule.Install(gas, _color);
        CapacityModule.Install(gas, MaxExtractorsPerGas);
    }

    private void AssignExtractor(Unit extractor) {
        extractor.Supervisor = this;
        extractor.AddDeathWatcher(this);

        _extractors.Add(extractor);

        DebugLocationModule.Install(extractor, _color);
        CapacityModule.Install(extractor, MaxPerExtractor);
        UnitModule.Get<CapacityModule>(_gasses.First(gas => gas.DistanceTo(extractor) < 1)).Assign(extractor);
    }

    private void AssignTownHall(Unit townHall) {
        townHall.Supervisor = this;
        townHall.AddDeathWatcher(this);

        TownHall = townHall;

        DebugLocationModule.Install(TownHall, _color);
    }
}
