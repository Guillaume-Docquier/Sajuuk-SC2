using System.Linq;
using Bot.UnitModules;

namespace Bot.Managers;

public partial class TownHallManager {
    private void LogRelease(Unit unit) {
        Logger.Debug("({0}) Released {1}", this, unit);
    }

    public void Release(Unit unit) {
        if (unit == null) {
            return;
        }

        if (Queen == unit) {
            ReleaseQueen(Queen);
        }
        else if (_workers.Contains(unit)) {
            ReleaseWorker(unit);
        }

        // TODO GD Implement release for minerals/gas/extractors?
    }

    private void ReleaseQueen(Unit queen) {
        if (queen == null) {
            Logger.Error("({0}) Trying to release a null queen", this);
            return;
        }

        if (Queen != queen) {
            Logger.Error("({0}) Trying to release a queen that isn't ours", this);
            return;
        }

        LogRelease(queen);

        queen.Supervisor = null;
        queen.RemoveDeathWatcher(this);

        Queen = null;

        UnitModule.Uninstall<DebugLocationModule>(queen);
        UnitModule.Uninstall<QueenMicroModule>(queen);
    }

    private void ReleaseWorker(Unit worker) {
        if (worker == null) {
            Logger.Error("({0}) Trying to release a null worker", this);
            return;
        }

        if (!_workers.Contains(worker)) {
            Logger.Error("({0}) Trying to release a worker that isn't ours", this);
            return;
        }

        LogRelease(worker);

        worker.Supervisor = null;
        worker.RemoveDeathWatcher(this);

        _workers.Remove(worker);

        UnitModule.Uninstall<DebugLocationModule>(worker);
        UnitModule.Uninstall<MiningModule>(worker);
    }

    private void ReleaseMineral(Unit mineral) {
        if (mineral == null) {
            Logger.Error("({0}) Trying to release a null mineral", this);
            return;
        }

        if (!_minerals.Contains(mineral)) {
            Logger.Error("({0}) Trying to release a mineral that isn't ours", this);
            return;
        }

        LogRelease(mineral);

        mineral.Supervisor = null;
        mineral.RemoveDeathWatcher(this);

        _minerals.Remove(mineral);

        UnitModule.Uninstall<DebugLocationModule>(mineral);
        var capacityModule = UnitModule.Uninstall<CapacityModule>(mineral);

        capacityModule.AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker).ReleaseResource());
    }

    private void ReleaseGas(Unit gas) {
        if (gas == null) {
            Logger.Error("({0}) Trying to release a null gas", this);
            return;
        }

        if (!_gasses.Contains(gas)) {
            Logger.Error("({0}) Trying to release a gas that isn't ours", this);
            return;
        }

        LogRelease(gas);

        gas.Supervisor = null;

        _gasses.Remove(gas);

        UnitModule.Uninstall<DebugLocationModule>(gas);
        var uselessExtractor = UnitModule.Uninstall<CapacityModule>(gas).AssignedUnits.FirstOrDefault();
        if (uselessExtractor != null) {
            ReleaseExtractor(uselessExtractor);
        }
    }

    private void ReleaseExtractor(Unit extractor) {
        if (extractor == null) {
            Logger.Error("({0}) Trying to release a null extractor", this);
            return;
        }

        if (!_extractors.Contains(extractor)) {
            Logger.Error("({0}) Trying to release an extractor that isn't ours", this);
            return;
        }

        LogRelease(extractor);

        extractor.Supervisor = null;
        extractor.RemoveDeathWatcher(this);

        _extractors.Remove(extractor);

        UnitModule.Uninstall<DebugLocationModule>(extractor);
        var capacityModule = UnitModule.Uninstall<CapacityModule>(extractor);

        capacityModule.AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker).ReleaseResource());
    }

    private void ReleaseTownHall(Unit townHall) {
        if (townHall == null) {
            Logger.Error("({0}) Trying to release a null townHall", this);
            return;
        }

        if (TownHall != townHall) {
            Logger.Error("({0}) Trying to release a townHall that isn't ours", this);
            return;
        }

        LogRelease(townHall);

        townHall.Supervisor = null;
        townHall.RemoveDeathWatcher(this);

        TownHall = null;

        UnitModule.Uninstall<DebugLocationModule>(townHall);
    }
}
