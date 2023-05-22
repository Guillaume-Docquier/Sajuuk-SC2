using Bot.GameSense;

namespace Bot.GameData;

public class PrerequisiteFactory : IPrerequisiteFactory {
    private readonly IUnitsTracker _unitsTracker;

    public PrerequisiteFactory(IUnitsTracker unitsTracker) {
        _unitsTracker = unitsTracker;
    }

    public UnitPrerequisite CreateUnitPrerequisite(uint unitType) {
        return new UnitPrerequisite(_unitsTracker, unitType);
    }

    public TechPrerequisite CreateTechPrerequisite(uint upgradeId) {
        return new TechPrerequisite(upgradeId);
    }
}
