namespace Bot.GameData;

public interface IPrerequisiteFactory {
    public UnitPrerequisite CreateUnitPrerequisite(uint unitType);
    public TechPrerequisite CreateTechPrerequisite(uint upgradeId);
}
