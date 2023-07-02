using System.Collections.Generic;

namespace Sajuuk.GameData;

public class TechPrerequisite : IPrerequisite {
    private readonly uint _upgradeId;

    public TechPrerequisite(uint upgradeId) {
        _upgradeId = upgradeId;
    }

    public bool IsMet(IEnumerable<Unit> ownedUnits, HashSet<uint> researchedUpgrades) {
        return researchedUpgrades.Contains(_upgradeId);
    }
}
