using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers;

public sealed partial class EconomyManager {
    private class EconomyManagerAssigner: IAssigner {
        private readonly EconomyManager _manager;

        public EconomyManagerAssigner(EconomyManager manager) {
            _manager = manager;
        }

        public void Assign(Unit unit) {
            var assigned = false;

            switch (unit.UnitType) {
                case Units.Hatchery:
                case Units.Lair:
                case Units.Hive:
                    assigned = AssignTownHall(unit);
                    break;
                case Units.Queen:
                case Units.QueenBurrowed:
                    assigned = AssignQueen(unit);
                    break;
                case Units.Drone:
                case Units.DroneBurrowed:
                    assigned = AssignWorker(unit);
                    break;
                default:
                    Logger.Error("({0}) Tried to assign {1}, but we don't manage this unit type", _manager, unit);
                    break;
            }

            if (assigned) {
                Logger.Debug("({0}) Assigned {1}", _manager, unit);
            }
        }

        private bool AssignTownHall(Unit townHall) {
            _manager._townHalls.Add(townHall);

            return true;
        }

        private bool AssignQueen(Unit queen) {
            _manager._queens.Add(queen);

            QueenMicroModule.Install(queen);
            ChangelingTargetingModule.Install(queen);

            return true;
        }

        private bool AssignWorker(Unit worker) {
            _manager._workers.Add(worker);

            return true;
        }
    }
}
