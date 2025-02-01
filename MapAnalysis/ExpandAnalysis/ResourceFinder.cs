using System.Numerics;
using Algorithms;
using Algorithms.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.State;
using SC2Client.Trackers;

namespace MapAnalysis.ExpandAnalysis;

public class ResourceFinder : IResourceFinder {
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;

    private List<List<IUnit>>? _resourceClustersCache;

    public ResourceFinder(
        KnowledgeBase knowledgeBase,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker
    ) {
        _knowledgeBase = knowledgeBase;
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
    public IEnumerable<List<IUnit>> FindResourceClusters() {
        if (_resourceClustersCache == null) {
            // See note on MineralField450
            var minerals = UnitQueries.GetUnits(_unitsTracker.NeutralUnits, UnitTypeId.MineralFields.Except(new[] { UnitTypeId.MineralField450 }).ToHashSet());
            var gasses = UnitQueries.GetUnits(_unitsTracker.NeutralUnits, UnitTypeId.GasGeysers);
            var resources = minerals.Concat(gasses).ToList();

            _resourceClustersCache = Clustering.DBSCAN(resources, epsilon: 8, minPoints: 4)
                .clusters
                // Expand clusters have at least a gas, and a minimum of 5 minerals (maybe more, but at least 5)
                .Where(cluster => cluster.Any(resource => UnitTypeId.GasGeysers.Contains(resource.UnitType)))
                .Where(cluster => cluster.Count(resource => UnitTypeId.MineralFields.Contains(resource.UnitType)) >= 5)
                .ToList();
        }

        return _resourceClustersCache;
    }

    /// <summary>
    /// Gets the resource cluster whose center is closest to the given expand position.
    /// </summary>
    /// <param name="expandPosition"></param>
    /// <returns></returns>
    public HashSet<IUnit> FindExpandResources(Vector2 expandPosition) {
        return FindResourceClusters()
            .MinBy(cluster => _terrainTracker.GetClosestWalkable(Clustering.GetCenter(cluster), searchRadius: 3).DistanceTo(expandPosition))!
            .ToHashSet();
    }

    /// <summary>
    /// Gets the neutral units that cover any cell needed to build a town hall at the given expand position.
    /// </summary>
    /// <param name="expandLocation"></param>
    /// <returns></returns>
    public HashSet<IUnit> FindExpandBlockers(Vector2 expandLocation) {
        var hatcheryRadius = _knowledgeBase.GetBuildingRadius(UnitTypeId.Hatchery);

        return _unitsTracker.NeutralUnits
            .Where(neutralUnit => neutralUnit.Distance2DTo(expandLocation) <= neutralUnit.Radius + hatcheryRadius)
            .ToHashSet();
    }
}
