using System.Collections.Generic;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot {
    public class MiningBot: PoliteBot {
        public override string Name => "MiningBot";

        public override Race Race => Race.Zerg;

        protected override void DoOnFrame() {
            // TODO GD Later on, need to target Hatch, Lair and Hives
            var hatcheries = Controller.GetUnits(Units.Hatchery);

            // Spawn overlords when needed
            if (Controller.AvailableSupply == 0) {
                // TODO GD Doesn't work, need 'LarvaTrain' ability on Larva
                hatcheries[0].Train(Units.Overlord);
            }

            // Spawn workers until full
            foreach (var hatchery in hatcheries) {
                if (hatchery.AssignedWorkers < hatchery.IdealWorkers) {
                    // TODO GD Doesn't work, need 'LarvaTrain' ability on Larva
                    hatchery.Train(Units.Drone);
                }
            }

            // Fast mining
        }
    }
}
