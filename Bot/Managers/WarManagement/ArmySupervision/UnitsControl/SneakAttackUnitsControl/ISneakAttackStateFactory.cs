namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public interface ISneakAttackStateFactory {
    public SneakAttack.ApproachState CreateApproachState();
    public SneakAttack.EngageState CreateEngageState();
    public SneakAttack.InactiveState CreateInactiveState();
    public SneakAttack.SetupState CreateSetupState();
    public SneakAttack.TerminalState CreateTerminalState();
}
