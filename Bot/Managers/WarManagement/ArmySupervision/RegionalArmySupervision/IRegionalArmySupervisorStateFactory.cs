namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public interface IRegionalArmySupervisorStateFactory {
    public ApproachState CreateApproachState();
    public DisengageState CreateDisengageState();
    public EngageState CreateEngageState();
}
