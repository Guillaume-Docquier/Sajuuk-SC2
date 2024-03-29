﻿using System.Collections.Generic;
using System.Linq;
using Sajuuk.GameData;

namespace Sajuuk.Managers.EconomyManagement.TownHallSupervision;

public partial class TownHallSupervisor {
    private const int MaxDistanceToExpand = 10;
    private const int MaxGas = 2;
    private const int MaxMinerals = 8;

    private IEnumerable<Unit> DiscoverMinerals() {
        return _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.MineralFields)
            .Where(mineral => mineral.Supervisor == null)
            .Where(mineral => mineral.GetRegion() == TownHall.GetRegion())
            .Where(mineral => mineral.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Take(MaxMinerals);
    }

    private IEnumerable<Unit> DiscoverGasses() {
        return _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.GasGeysers)
            .Where(gas => gas.Supervisor == null)
            .Where(gas => gas.GetRegion() == TownHall.GetRegion())
            .Where(gas => gas.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Where(gas => !IsGasDepleted(gas))
            .Take(MaxGas);
    }

    /// <summary>
    /// Discovers extractors that should be supervised by us.
    /// We pass IEnumerable<Unit> unitPool for performance reasons. The impact is probably minuscule, but it is easy to handle.
    /// </summary>
    /// <param name="unitPool">The unit pool to discover extractors from</param>
    /// <returns>A list of extractors to be supervised by us</returns>
    private IEnumerable<Unit> DiscoverExtractors(IEnumerable<Unit> unitPool) {
        if (_extractors.Count >= _gasses.Count) {
            return new List<Unit>();
        }

        return _unitsTracker.GetUnits(unitPool, Units.Extractor)
            .Where(extractor => extractor.Supervisor == null)
            .Where(extractor => _gasses.Any(gas => extractor.DistanceTo(gas) < 1)); // Should be 0, we chose 1 just in case
    }
}
