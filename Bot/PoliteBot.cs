using System.Collections.Generic;
using System.Linq;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot {
    public abstract class PoliteBot: IBot {
        public abstract string Name { get; }

        public abstract Race Race { get; }

        public IEnumerable<Action> OnFrame() {
            Controller.OpenFrame();

            EnsureGreeting();
            EnsureGG();

            DoOnFrame();

            return Controller.CloseFrame();
        }

        private void EnsureGreeting() {
            if (Controller.Frame == 0) {
                Logger.Info(Name);
                Logger.Info("--------------------------------------");
                Logger.Info("Map: {0}", Controller.GameInfo.MapName);
                Logger.Info("--------------------------------------");
            }

            if (Controller.Frame == Controller.SecsToFrames(1)) {
                Controller.Chat("gl hf");
            }
        }

        private void EnsureGG() {
            var structures = Controller.GetUnits(Controller.OwnedUnits, Units.Structures);
            if (structures.Count() == 1 && structures.First().Integrity < 0.4) {
                if (!Controller.ChatLog.Contains("gg wp")) {
                    Controller.Chat("gg wp");
                }
            }
        }

        protected abstract void DoOnFrame();
    }
}
