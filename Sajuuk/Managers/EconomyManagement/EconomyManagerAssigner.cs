﻿using Sajuuk.GameData;

namespace Sajuuk.Managers.EconomyManagement;

public sealed partial class EconomyManager {
    private class EconomyManagerAssigner: Assigner<EconomyManager> {
        public EconomyManagerAssigner(EconomyManager client) : base(client) {}

        public override void Assign(Unit unit) {
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
                    Logger.Error("({0}) Tried to assign {1}, but we don't manage this unit type", Client, unit);
                    break;
            }

            if (assigned) {
                Logger.Debug("({0}) Assigned {1}", Client, unit);
            }
        }

        private bool AssignTownHall(Unit townHall) {
            Client._townHalls.Add(townHall);

            return true;
        }

        private bool AssignQueen(Unit queen) {
            Client._unitModuleInstaller.InstallQueenMicroModule(queen, null);
            Client._unitModuleInstaller.InstallChangelingTargetingModule(queen);

            return true;
        }

        private bool AssignWorker(Unit worker) {
            Client._workers.Add(worker);

            return true;
        }
    }
}
