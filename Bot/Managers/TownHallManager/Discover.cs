using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers;

public partial class TownHallManager {
    private List<Unit> DiscoverMinerals() {
        return Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralFields)
            .Where(mineral => mineral.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Where(mineral => mineral.Supervisor == null)
            .Take(MaxMinerals)
            .ToList();
    }

    private List<Unit> DiscoverGasses() {
        return Controller.GetUnits(UnitsTracker.NeutralUnits, Units.GasGeysers)
            .Where(gas => gas.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Where(gas => gas.Supervisor == null)
            .Where(gas => !IsGasDepleted(gas))
            .Take(MaxGas)
            .ToList();
    }

    private List<Unit> DiscoverExtractors(IEnumerable<Unit> newUnits) {
        if (_extractors.Count >= _gasses.Count) {
            return new List<Unit>();
        }

        return Controller.GetUnits(newUnits, Units.Extractor)
            .Where(extractor => _gasses.Any(gas => extractor.DistanceTo(gas) < 1)) // Should be 0, we chose 1 just in case
            .Where(extractor => !_extractors.Contains(extractor)) // Safety check
            .ToList();
    }
}
