using System.Collections.Generic;
using System.Linq;

namespace Bot.GameData;

public interface IPrerequisite {
    bool IsMet(IEnumerable<Unit> ownedUnits, HashSet<uint> researchedUpgrades);
}

public class UnitPrerequisite : IPrerequisite {
    private readonly uint _unitType;

    public UnitPrerequisite(uint unitType) {
        _unitType = unitType;
    }

    public bool IsMet(IEnumerable<Unit> ownedUnits, HashSet<uint> researchedUpgrades) {
        return Controller.GetUnits(ownedUnits, _unitType).Any(unit => unit.IsOperational);
    }
}

public class TechPrerequisite : IPrerequisite {
    private readonly uint _upgradeId;

    public TechPrerequisite(uint upgradeId) {
        _upgradeId = upgradeId;
    }

    public bool IsMet(IEnumerable<Unit> ownedUnits, HashSet<uint> researchedUpgrades) {
        return researchedUpgrades.Contains(_upgradeId);
    }
}
