using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers;

public sealed partial class EconomyManager {
    private class EconomyManagerReleaser: IReleaser {
        private readonly EconomyManager _manager;

        public EconomyManagerReleaser(EconomyManager manager) {
            _manager = manager;
        }

        public void Release(Unit unit) {
            var released = false;

            switch (unit.UnitType) {
                case Units.Hatchery:
                case Units.Lair:
                case Units.Hive:
                    released = ReleaseTownHall(unit);
                    break;
                case Units.Queen:
                case Units.QueenBurrowed:
                    released = ReleaseQueen(unit);
                    break;
                case Units.Drone:
                case Units.DroneBurrowed:
                    released = ReleaseWorker(unit);
                    break;
                default:
                    Logger.Error("({0}) Tried to release {1}, but we don't manage this unit type", _manager, unit);
                    break;
            }

            if (released) {
                Logger.Debug("({0}) Released {1}", _manager, unit);
            }
        }

        private bool ReleaseTownHall(Unit townHall) {
            _manager._townHalls.Remove(townHall);

            var supervisor = townHall.Supervisor;
            if (supervisor is TownHallSupervisor townHallSupervisor && _manager._townHallSupervisors.Remove(townHallSupervisor)) {
                supervisor.Retire();
            }
            else {
                Logger.Error("({0}) Trying to retire supervisor {1} while releasing {2}, but it isn't ours", this, supervisor, townHall);
            }

            return true;
        }

        private bool ReleaseQueen(Unit queen) {
            _manager._queens.Remove(queen);

            UnitModule.Uninstall<QueenMicroModule>(queen);
            UnitModule.Uninstall<ChangelingTargetingModule>(queen);

            return true;
        }

        private bool ReleaseWorker(Unit worker) {
            _manager._workers.Remove(worker);

            return true;
        }
    }
}
