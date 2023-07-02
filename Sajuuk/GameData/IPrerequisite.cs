using System.Collections.Generic;

namespace Sajuuk.GameData;

public interface IPrerequisite {
    bool IsMet(IEnumerable<Unit> ownedUnits, HashSet<uint> researchedUpgrades);
}
