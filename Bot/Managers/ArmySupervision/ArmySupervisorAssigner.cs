using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class ArmySupervisorAssigner: IAssigner {
        private readonly ArmySupervisor _supervisor;

        public ArmySupervisorAssigner(ArmySupervisor supervisor) {
            _supervisor = supervisor;
        }

        public void Assign(Unit unit) {
            // TODO GD Use a targeting module
            _supervisor.Army.Add(unit);

            if (unit.UnitType is Units.Roach or Units.RoachBurrowed) {
                BurrowMicroModule.Install(unit);
            }

            AttackPriorityModule.Install(unit);

            Logger.Debug("({0}) Assigned {1}", _supervisor, unit);
        }
    }
}
