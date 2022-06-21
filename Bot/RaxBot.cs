using System.Collections.Generic;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot {
    internal class RaxBot : IBot {
        public string Name => "RaxBot";

        public Race Race => Race.Terran;

        //the following will be called every frame
        //you can increase the amount of frames that get processed for each step at once in Wrapper/GameConnection.cs: stepSize
        public IEnumerable<Action> OnFrame() {
            Controller.OpenFrame();

            if (Controller.Frame == 0) {
                Logger.Info("RaxBot");
                Logger.Info("--------------------------------------");
                Logger.Info("Map: {0}", Controller.GameInfo.MapName);
                Logger.Info("--------------------------------------");
            }

            if (Controller.Frame == Controller.SecsToFrames(1)) {
                Controller.Chat("gl hf");
            }

            var structures = Controller.GetUnits(Units.Structures);
            // Last building
            if (structures.Count == 1) {
                // Being attacked or burning down
                if (structures[0].Integrity < 0.4) {
                    if (!Controller.ChatLog.Contains("gg")) {
                        Controller.Chat("gg");
                    }
                }
            }

            var resourceCenters = Controller.GetUnits(Units.ResourceCenters);
            foreach (var rc in resourceCenters) {
                if (Controller.CanConstruct(Units.Scv)) {
                    rc.Train(Units.Scv);
                }
            }

            //keep on buildings depots if supply is tight
            if (Controller.MaxSupply - Controller.CurrentSupply <= 5) {
                if (Controller.CanConstruct(Units.SupplyDepot)) {
                    if (Controller.GetPendingCount(Units.SupplyDepot) == 0) {
                        Controller.Construct(Units.SupplyDepot);
                    }
                }
            }

            //distribute workers optimally every 10 frames
            if (Controller.Frame % 10 == 0) {
                Controller.DistributeWorkers();
            }

            //build up to 4 barracks at once
            if (Controller.CanConstruct(Units.Barracks)) {
                if (Controller.GetTotalCount(Units.Barracks) < 4) {
                    Controller.Construct(Units.Barracks);
                }
            }

            //train marine
            foreach (var barracks in Controller.GetUnits(Units.Barracks, onlyCompleted: true)) {
                if (Controller.CanConstruct(Units.Marine)) {
                    barracks.Train(Units.Marine);
                }
            }

            //attack when we have enough units
            var army = Controller.GetUnits(Units.ArmyUnits);
            if (army.Count > 20) {
                if (Controller.EnemyLocations.Count > 0) {
                    Controller.Attack(army, Controller.EnemyLocations[0]);
                }
            }

            return Controller.CloseFrame();
        }
    }
}
