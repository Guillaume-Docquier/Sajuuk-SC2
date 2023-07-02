using Sajuuk.GameData;
using Sajuuk.Managers.EconomyManagement.TownHallSupervision;
using Sajuuk.UnitModules;

namespace Sajuuk.Managers.EconomyManagement;

public sealed partial class EconomyManager {
    private class EconomyManagerReleaser: Releaser<EconomyManager> {
        public EconomyManagerReleaser(EconomyManager client) : base(client) {}

        public override void Release(Unit unit) {
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
                    Logger.Error("({0}) Tried to release {1}, but we don't manage this unit type", Client, unit);
                    break;
            }

            if (released) {
                Logger.Debug("({0}) Released {1}", Client, unit);
            }
        }

        private bool ReleaseTownHall(Unit townHall) {
            Client._townHalls.Remove(townHall);

            var supervisor = townHall.Supervisor;
            if (supervisor is TownHallSupervisor townHallSupervisor && Client._townHallSupervisors.Remove(townHallSupervisor)) {
                supervisor.Retire();
            }
            else {
                Logger.Error("({0}) Trying to retire supervisor {1} while releasing {2}, but it isn't ours", this, supervisor, townHall);
            }

            return true;
        }

        private static bool ReleaseQueen(Unit queen) {
            UnitModule.Uninstall<QueenMicroModule>(queen);
            UnitModule.Uninstall<ChangelingTargetingModule>(queen);

            return true;
        }

        private bool ReleaseWorker(Unit worker) {
            Client._workers.Remove(worker);

            return true;
        }
    }
}
