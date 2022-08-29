using System.Collections.Generic;
using Bot.GameData;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

public sealed partial class EconomyManager {
    private class EconomyManagerDispatcher: IDispatcher {
        private static readonly List<Color> SupervisorColors = new List<Color>
        {
            Colors.MaroonRed,
            Colors.BurlywoodBeige,
            Colors.CornflowerBlue,
            Colors.DarkGreen,
            Colors.DarkBlue,
        };

        private readonly EconomyManager _manager;

        public EconomyManagerDispatcher(EconomyManager manager) {
            _manager = manager;
        }

        public void Dispatch(Unit unit) {
            var dispatched = false;

            switch (unit.UnitType) {
                case Units.Hatchery:
                case Units.Lair:
                case Units.Hive:
                    dispatched = DispatchTownHall(unit);
                    break;
                case Units.Queen:
                case Units.QueenBurrowed:
                    dispatched = DispatchQueen(unit);
                    break;
                case Units.Drone:
                case Units.DroneBurrowed:
                    dispatched = DispatchWorker(unit);
                    break;
                default:
                    Logger.Error("({0}) Tried to dispatch {1}, but we don't manage this unit type", _manager, unit);
                    break;
            }

            if (dispatched) {
                Logger.Debug("({0}) Dispatched {1}", _manager, unit);
            }
        }

        private bool DispatchTownHall(Unit townHall) {
            // TODO GD Assign a color to each region/expand instead?
            var newExpandColor = SupervisorColors[_manager._townHalls.Count % SupervisorColors.Count];

            var townHallSupervisor = TownHallSupervisor.Create(townHall, newExpandColor);
            _manager._townHallSupervisors.Add(townHallSupervisor);

            return true;
        }

        private bool DispatchQueen(Unit queen) {
            var supervisor = _manager.GetClosestSupervisorWithNoQueen(queen);

            if (supervisor == null) {
                return false;
            }

            supervisor.Assign(queen);

            return true;
        }

        private bool DispatchWorker(Unit worker) {
            var supervisor = _manager.GetClosestSupervisorWithIdealCapacityNotMet(worker.Position);
            supervisor ??= _manager.GetClosestSupervisorWithSaturatedCapacityNotMet(worker.Position);
            supervisor ??= _manager.GetSupervisorWithHighestAvailableCapacity();

            if (supervisor == null) {
                return false;
            }

            supervisor.Assign(worker);

            return true;
        }
    }
}
