using System.Collections.Generic;

namespace Bot.GameData;

public interface IPrerequisite {
    bool IsMet(IEnumerable<Unit> ownedUnits, HashSet<uint> researchedUpgrades);
}
