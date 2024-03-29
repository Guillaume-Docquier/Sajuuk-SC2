﻿namespace Sajuuk.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    private class ArmySupervisorAssigner: IAssigner {
        private readonly ArmySupervisor _supervisor;

        public ArmySupervisorAssigner(ArmySupervisor supervisor) {
            _supervisor = supervisor;
        }

        public void Assign(Unit unit) {
            _supervisor.Army.Add(unit);
            Logger.Debug("({0}) Assigned {1}", _supervisor, unit);
        }
    }
}
