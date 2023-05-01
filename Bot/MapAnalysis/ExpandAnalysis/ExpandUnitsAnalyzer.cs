using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.MapAnalysis.ExpandAnalysis;

public class ExpandUnitsAnalyzer : IExpandUnitsAnalyzer {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;

    private List<List<Unit>> _resourceClusters;

    public ExpandUnitsAnalyzer(IUnitsTracker unitsTracker, ITerrainTracker terrainTracker) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
    }

    /// <summary>
    /// Gets all the resource clusters around expands.
    /// We exclude MineralField450 because they are small patches usually used to obstruct space.
    ///
    /// We cache the result because resources don't change and clustering is costly to run.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<List<Unit>> FindResourceClusters() {
        if (_resourceClusters == null) {
            // See note on MineralField450
            var minerals = Controller.GetUnits(_unitsTracker.NeutralUnits, Units.MineralFields.Except(new[] { Units.MineralField450 }).ToHashSet());
            var gasses = Controller.GetUnits(_unitsTracker.NeutralUnits, Units.GasGeysers);
            var resources = minerals.Concat(gasses).ToList();

            _resourceClusters = Clustering.Instance.DBSCAN(resources, epsilon: 8, minPoints: 4).clusters;
        }

        return _resourceClusters;
    }

    /// <summary>
    /// Gets the resource cluster whose center is closest to the given expand position.
    /// </summary>
    /// <param name="expandPosition"></param>
    /// <returns></returns>
    public HashSet<Unit> FindExpandResources(Vector2 expandPosition) {
        return FindResourceClusters()
            .MinBy(cluster => _terrainTracker.GetClosestWalkable(cluster.GetCenter(), searchRadius: 3).DistanceTo(expandPosition))!
            .ToHashSet();
    }

    /// <summary>
    /// Gets the neutral units that cover any cell needed to build a town hall at the given expand position.
    /// </summary>
    /// <param name="expandLocation"></param>
    /// <returns></returns>
    public HashSet<Unit> FindExpandBlockers(Vector2 expandLocation) {
        var hatcheryRadius = KnowledgeBase.GetBuildingRadius(Units.Hatchery);

        return _unitsTracker.NeutralUnits
            .Where(neutralUnit => neutralUnit.DistanceTo(expandLocation) <= neutralUnit.Radius + hatcheryRadius)
            .ToHashSet();
    }
}
