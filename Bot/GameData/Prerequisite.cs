using System.Linq;
using Bot.GameSense;

namespace Bot.GameData;

public interface IPrerequisite {
    bool IsMet();
}

public class UnitPrerequisite : IPrerequisite {
    private readonly uint _unitType;

    public UnitPrerequisite(uint unitType) {
        _unitType = unitType;
    }

    public bool IsMet() {
        return Controller.GetUnits(UnitsTracker.OwnedUnits, _unitType).Any(unit => unit.IsOperational);
    }
}

public class TechPrerequisite : IPrerequisite {
    private readonly uint _upgradeId;

    public TechPrerequisite(uint upgradeId) {
        _upgradeId = upgradeId;
    }

    public bool IsMet() {
        return Controller.ResearchedUpgrades.Contains(_upgradeId);
    }
}
