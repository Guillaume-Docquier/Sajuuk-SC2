using System.Collections.Generic;
using System.Linq;
using Bot.GameSense;

namespace Bot.GameData;

public class UnitPrerequisite : IPrerequisite {
    private readonly IUnitsTracker _unitsTracker;
    private readonly uint _unitType;

    public UnitPrerequisite(IUnitsTracker unitsTracker, uint unitType) {
        _unitsTracker = unitsTracker;
        _unitType = unitType;
    }

    public bool IsMet(IEnumerable<Unit> ownedUnits, HashSet<uint> researchedUpgrades) {
        return _unitsTracker.GetUnits(ownedUnits, _unitType).Any(unit => unit.IsOperational);
    }
}
