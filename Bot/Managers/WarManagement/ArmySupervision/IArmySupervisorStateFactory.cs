namespace Bot.Managers.WarManagement.ArmySupervision;

public interface IArmySupervisorStateFactory {
    public ArmySupervisor.AttackState CreateAttackState();
    public ArmySupervisor.DefenseState CreateDefenseState();
    public ArmySupervisor.HuntState CreateHuntState();
    public ArmySupervisor.RallyState CreateRallyState();
}
