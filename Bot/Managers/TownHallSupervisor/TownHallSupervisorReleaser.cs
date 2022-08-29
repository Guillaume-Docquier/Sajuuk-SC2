using System.Linq;
using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers;

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
            _supervisor.TownHall = null;

            UnitModule.Uninstall<DebugLocationModule>(townHall);

            LogRelease(townHall);
        }

        private void ReleaseQueen(Unit queen) {
            _supervisor.Queen = null;

            UnitModule.Uninstall<DebugLocationModule>(queen);

            LogRelease(queen);
        }

        private void ReleaseWorker(Unit worker) {
            _supervisor._workers.Remove(worker);

            UnitModule.Uninstall<DebugLocationModule>(worker);
            UnitModule.Uninstall<MiningModule>(worker);

            LogRelease(worker);
        }

        private void ReleaseMineral(Unit mineral) {
            mineral.RemoveDeathWatcher(_supervisor); // TODO GD Should have a mechanism to track deathwatch for safer/easier cleaning

            _supervisor._minerals.Remove(mineral);

            UnitModule.Uninstall<DebugLocationModule>(mineral);
            var capacityModule = UnitModule.Uninstall<CapacityModule>(mineral);

            capacityModule.AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker).ReleaseResource());

            LogRelease(mineral);
        }

        private void ReleaseGas(Unit gas) {
            gas.RemoveDeathWatcher(_supervisor);

            _supervisor._gasses.Remove(gas);

            UnitModule.Uninstall<DebugLocationModule>(gas);
            var uselessExtractor = UnitModule.Uninstall<CapacityModule>(gas).AssignedUnits.FirstOrDefault();
            if (uselessExtractor != null) {
                ReleaseExtractor(uselessExtractor); // TODO GD Release or change capacity to 0?
            }

            LogRelease(gas);
        }

        private void ReleaseExtractor(Unit extractor) {
            _supervisor._extractors.Remove(extractor);

            UnitModule.Uninstall<DebugLocationModule>(extractor);
            var capacityModule = UnitModule.Uninstall<CapacityModule>(extractor);

            capacityModule.AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker).ReleaseResource());

            LogRelease(extractor);
        }
    }
}
