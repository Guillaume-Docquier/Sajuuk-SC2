﻿using System.Collections.Generic;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using SC2APIProtocol;

namespace Sajuuk.Managers.EconomyManagement;

public sealed partial class EconomyManager {
    private class EconomyManagerDispatcher: Dispatcher<EconomyManager> {
        private static readonly List<Color> SupervisorColors = new List<Color>
        {
            Colors.MaroonRed,
            Colors.BurlywoodBeige,
            Colors.CornflowerBlue,
            Colors.DarkGreen,
            Colors.DarkBlue,
        };

        public EconomyManagerDispatcher(EconomyManager client) : base(client) {}

        public override void Dispatch(Unit unit) {
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
                    Logger.Error("({0}) Tried to dispatch {1}, but we don't manage this unit type", Client, unit);
                    break;
            }

            if (dispatched) {
                Logger.Debug("({0}) Dispatched {1}", Client, unit);
            }
        }

        private bool DispatchTownHall(Unit townHall) {
            // TODO GD Assign a color to each region/expand instead?
            var newExpandColor = SupervisorColors[Client._townHalls.Count % SupervisorColors.Count];

            var townHallSupervisor = Client._economySupervisorFactory.CreateTownHallSupervisor(townHall, newExpandColor);
            Client._townHallSupervisors.Add(townHallSupervisor);

            return true;
        }

        private bool DispatchQueen(Unit queen) {
            var supervisor = Client.GetClosestSupervisorWithNoQueen(queen);

            if (supervisor == null) {
                return false;
            }

            supervisor.Assign(queen);

            return true;
        }

        private bool DispatchWorker(Unit worker) {
            var supervisor = Client.GetClosestSupervisorWithIdealCapacityNotMet(worker.Position.ToVector2());
            supervisor ??= Client.GetClosestSupervisorWithSaturatedCapacityNotMet(worker.Position.ToVector2());

            if (supervisor == null) {
                Client._graphicalDebugger.AddText("!", color: Colors.Red, worldPos: worker.Position.ToPoint(yOffset: worker.Radius));
                return false;
            }

            supervisor.Assign(worker);

            return true;
        }
    }
}
