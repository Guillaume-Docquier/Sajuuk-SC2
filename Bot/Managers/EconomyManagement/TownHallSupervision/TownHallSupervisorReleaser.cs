using System.Linq;
using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers.EconomyManagement.TownHallSupervision;

public partial class TownHallSupervisor {
    private class TownHallSupervisorReleaser: IReleaser {
        private readonly TownHallSupervisor _supervisor;

        public TownHallSupervisorReleaser(TownHallSupervisor supervisor) {
            _supervisor = supervisor;
        }

        public void Release(Unit unit) {
            switch (unit.UnitType) {
                case Units.Hatchery:
                case Units.Lair:
                case Units.Hive:
                    ReleaseTownHall(unit);
                    return;
                case Units.Queen:
                case Units.QueenBurrowed:
                    ReleaseQueen(unit);
                    return;
                case Units.Drone:
                case Units.DroneBurrowed:
                    ReleaseWorker(unit);
                    return;
                case Units.Extractor:
                    ReleaseExtractor(unit);
                    return;
            }

            if (Units.MineralFields.Contains(unit.UnitType)) {
                ReleaseMineral(unit);
                return;
            }

            if (Units.GasGeysers.Contains(unit.UnitType)) {
                ReleaseGas(unit);
                return;
            }

            Logger.Error("({0}) Tried to release {1}, but we don't supervise this unit type", _supervisor, unit);
        }

        private void LogRelease(Unit unit) {
            Logger.Debug("({0}) Released {1}", _supervisor, unit);
        }

        private void ReleaseTownHall(Unit townHall) {
            LogRelease(townHall);

            _supervisor.TownHall = null;

            UnitModule.Uninstall<DebugLocationModule>(townHall);
        }

        private void ReleaseQueen(Unit queen) {
            LogRelease(queen);

            _supervisor.Queen = null;

            UnitModule.Uninstall<DebugLocationModule>(queen);
        }

        private void ReleaseWorker(Unit worker) {
            LogRelease(worker);

            _supervisor._workers.Remove(worker);

            UnitModule.Uninstall<DebugLocationModule>(worker);
            UnitModule.Uninstall<MiningModule>(worker);
        }

        private void ReleaseMineral(Unit mineral) {
            LogRelease(mineral);

            mineral.RemoveDeathWatcher(_supervisor);

            _supervisor._minerals.Remove(mineral);

            UnitModule.Uninstall<DebugLocationModule>(mineral);
            var capacityModule = UnitModule.Uninstall<CapacityModule>(mineral);

            capacityModule.AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker).ReleaseResource());
        }

        private void ReleaseGas(Unit gas) {
            LogRelease(gas);

            gas.RemoveDeathWatcher(_supervisor);

            _supervisor._gasses.Remove(gas);

            UnitModule.Uninstall<DebugLocationModule>(gas);

            var uselessExtractor = UnitModule.Uninstall<CapacityModule>(gas).AssignedUnits.FirstOrDefault();
            uselessExtractor?.Supervisor.Release(uselessExtractor);
        }

        private void ReleaseExtractor(Unit extractor) {
            LogRelease(extractor);

            _supervisor._extractors.Remove(extractor);

            UnitModule.Uninstall<DebugLocationModule>(extractor);
            var capacityModule = UnitModule.Uninstall<CapacityModule>(extractor);

            // TODO GD Something can be null here, that's odd
            // TODO GD Understand why and fix it
            capacityModule?.AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker)?.ReleaseResource());
        }
    }
}
