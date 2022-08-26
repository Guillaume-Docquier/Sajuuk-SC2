using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

// TODO GD This is a supervisor
public partial class TownHallManager: IManager {
    private const int MaxGas = 2;
    private const int MaxExtractorsPerGas = 1;
    private const int MaxPerExtractor = 3;
    private const int MaxMinerals = 8;
    private const int IdealPerMinerals = 2;
    private const int MaxPerMinerals = 3;
    private const int MaxDistanceToExpand = 10;

    private readonly Color _color;
    public Unit TownHall { get; private set; }
    public Unit Queen { get; private set; }

    private readonly List<Unit> _minerals = new List<Unit>();
    private readonly List<Unit> _gasses = new List<Unit>();
    private readonly List<Unit> _extractors = new List<Unit>();

    // TODO GD Make this a hashset?
    private readonly List<Unit> _workers = new List<Unit>();

    private bool _expandHasBeenRequested = false;
    private readonly int _initialMineralsSum;

    private readonly BuildRequest _expandBuildRequest = new QuantityBuildRequest(BuildType.Expand, Units.Hatchery, atSupply: 75, quantity: 0);
    private readonly List<BuildRequest> _buildStepRequests = new List<BuildRequest>();

    public IEnumerable<BuildFulfillment> BuildFulfillments => _buildStepRequests.Select(buildRequest => buildRequest.Fulfillment);

    public int IdealCapacity => _minerals.Count * IdealPerMinerals + _extractors.Count(extractor => extractor.IsOperational) * MaxPerExtractor;
    public int IdealAvailableCapacity => IdealCapacity - _workers.Count;

    public int SaturatedCapacity => IdealCapacity + _minerals.Count; // Can allow 1 more per mineral patch;
    public int SaturatedAvailableCapacity => SaturatedCapacity - _workers.Count;

    public int WorkerCount => _workers.Count;

    public TownHallManager(Unit townHall, Color color) {
        _color = color;

        AssignTownHall(townHall);

        DiscoverMinerals().ForEach(AssignMineral);
        DiscoverGasses().ForEach(AssignGas);
        DiscoverExtractors(UnitsTracker.OwnedUnits).ForEach(AssignExtractor);

        _buildStepRequests.Add(_expandBuildRequest);

        // You're a macro hatch
        if (_minerals.Count == 0) {
            _expandHasBeenRequested = true;
        }

        // TODO GD Put this in Expand analyzer, and try to find max minerals based on patch type?
        _initialMineralsSum = _minerals.Sum(mineral => mineral.InitialMineralCount);
    }

    public void OnFrame() {
        DrawTownHallInfo();

        HandleDepletedGasses();
        DiscoverExtractors(UnitsTracker.NewOwnedUnits).ForEach(AssignExtractor);

        DispatchWorkers(GetAssignedIdleWorkers());

        if (ShouldFillExtractors()) {
            FillExtractors();
        }

        RequestExpand();
    }

    public void Retire() {
        ReleaseTownHall(TownHall);

        _minerals.ToList().ForEach(ReleaseMineral);
        _gasses.ToList().ForEach(ReleaseGas);
        _extractors.ToList().ForEach(ReleaseExtractor);
        _workers.ToList().ForEach(ReleaseWorker);

        if (Queen != null) {
            ReleaseQueen(Queen);
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (deadUnit.UnitType == Units.Drone) {
            _workers.Remove(deadUnit);
        }
        else if (Units.MineralFields.Contains(deadUnit.UnitType)) {
            _minerals.Remove(deadUnit);
            UnitModule.Get<CapacityModule>(deadUnit).AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker).ReleaseResource());
        }
        else if (deadUnit.UnitType == Units.Extractor) {
            _extractors.Remove(deadUnit);
            UnitModule.Get<CapacityModule>(deadUnit).AssignedUnits.ForEach(worker => UnitModule.Get<MiningModule>(worker).ReleaseResource());
        }
        else if (deadUnit.UnitType == Units.Queen) {
            Queen = null;
        }
    }

    private List<Unit> GetAssignedIdleWorkers() {
        return _workers.Where(worker => UnitModule.Get<MiningModule>(worker).AssignedResource == null).ToList();
    }

    /// <summary>
    /// Give a mining assignment to each provided worker.
    /// </summary>
    /// <param name="workers">The workers to give a mining assignment to.</param>
    private void DispatchWorkers(List<Unit> workers) {
        workers.ForEach(DispatchWorker);
    }

    /// <summary>
    /// Give a mining assignment to the provided worker.
    /// </summary>
    /// <param name="worker">The worker to give a mining assignment to.</param>
    private void DispatchWorker(Unit worker) {
        var assignedResource = GetClosestExtractorWithAvailableCapacity(worker);
        assignedResource ??= GetClosestMineralWithAvailableCapacity(worker, minAvailableCapacity: 1);
        assignedResource ??= GetClosestMineralWithAvailableCapacity(worker, minAvailableCapacity: 0);
        assignedResource ??= GetClosestMineralWithAvailableCapacity(worker, minAvailableCapacity: -999);

        if (assignedResource != null) {
            UpdateWorkerMiningAssignment(worker, assignedResource);
        }
    }

    private Unit GetClosestExtractorWithAvailableCapacity(Unit worker) {
        return _extractors
            .Where(extractor => extractor.IsOperational)
            .Where(extractor => UnitModule.Get<CapacityModule>(extractor).AvailableCapacity > 0)
            .MinBy(extractor => extractor.DistanceTo(worker));
    }

    private Unit GetClosestMineralWithAvailableCapacity(Unit worker, int minAvailableCapacity) {
        return _minerals
            .Where(mineral => UnitModule.Get<CapacityModule>(mineral).AvailableCapacity > minAvailableCapacity)
            .MinBy(mineral => mineral.DistanceTo(worker));
    }

    private bool ShouldFillExtractors() {
        return _minerals.Sum(mineral => UnitModule.Get<CapacityModule>(mineral).AvailableCapacity) <= _minerals.Count;
    }

    /// <summary>
    /// Force workers to work in extractors if we have enough already mining.
    /// </summary>
    private void FillExtractors() {
        var extractorsToFill = _extractors
            .Where(extractor => extractor.IsOperational)
            .Where(extractor => UnitModule.Get<CapacityModule>(extractor).AvailableCapacity > 0);

        foreach (var extractor in extractorsToFill) {
            var workersToReassign = _workers
                .Where(worker => UnitModule.Get<MiningModule>(worker).ResourceType == UnitUtils.ResourceType.Mineral)
                .OrderBy(worker => {
                    var resource = UnitModule.Get<MiningModule>(worker).AssignedResource;

                    return UnitModule.Get<CapacityModule>(resource).AvailableCapacity;
                })
                .Take(UnitModule.Get<CapacityModule>(extractor).AvailableCapacity);

            foreach (var worker in workersToReassign) {
                UpdateWorkerMiningAssignment(worker, extractor);
            }
        }
    }

    /// <summary>
    /// Assigns a worker to a resource and releases the worker from its previously assigned resource, if any.
    /// </summary>
    /// <param name="worker">The worker to update the mining assignment of.</param>
    /// <param name="assignedResource">The new resource to mine</param>
    private static void UpdateWorkerMiningAssignment(Unit worker, Unit assignedResource) {
        var miningModule = UnitModule.Get<MiningModule>(worker);

        UnitModule.Get<CapacityModule>(miningModule.AssignedResource)?.Release(worker);
        UnitModule.Get<CapacityModule>(assignedResource).Assign(worker);

        miningModule.ReleaseResource();
        miningModule.AssignResource(assignedResource);
    }

    private static bool IsGasDepleted(Unit gas) {
        return gas.RawUnitData.DisplayType != DisplayType.Snapshot && gas.RawUnitData.VespeneContents <= 0;
    }

    private void HandleDepletedGasses() {
        _gasses.Where(IsGasDepleted).ToList().ForEach(ReleaseGas);
    }

    /// <summary>
    /// Requests to build an expand once the remaining minerals reaches a certain threshold.
    /// We can only request an expand if the TownHall is operational and we request a maximum of 1 expand.
    /// </summary>
    private void RequestExpand() {
        // TODO GD This doesn't count depleted minerals
        // TODO GD Should probably put this in expand analyzer, but it's fine for now
        if (TownHall.IsOperational && !_expandHasBeenRequested && (GetRemainingMineralsPercent() <= 0.6 || _minerals.Count <= 5)) {
            _expandBuildRequest.Requested += 1;

            _expandHasBeenRequested = true;
        }
    }

    /// <summary>
    /// Adds DebugTexts about the TownHall, if it is operational.
    /// </summary>
    private void DrawTownHallInfo() {
        if (!TownHall.IsOperational) {
            return;
        }

        GraphicalDebugger.AddTextGroup(new[]
            {
                $"IdealAvailableCapacity: {IdealAvailableCapacity}",
                $"SaturatedAvailableCapacity: {SaturatedAvailableCapacity}",
                $"RemainingMineralsPercent: {GetRemainingMineralsPercent():P}",
            },
            worldPos: TownHall.Position.Translate(xTranslation: -2.5f, yTranslation: 1f).ToPoint());
    }

    private float GetRemainingMineralsPercent() {
        if (_initialMineralsSum == 0) {
            return 0;
        }

        return (float)_minerals.Sum(mineral => mineral.RawUnitData.MineralContents) / _initialMineralsSum;
    }

    /// <summary>
    /// Find workers to free for someone else to use.
    /// </summary>
    /// <param name="count">The number of workers to free</param>
    /// <returns>The list of freed workers</returns>
    public IEnumerable<Unit> HandOutWorkers(int count) {
        foreach (var idleWorker in GetAssignedIdleWorkers().Take(count).ToList()) {
            ReleaseWorker(idleWorker);
            yield return idleWorker;
            count -= 1;
        }

        if (_minerals.Count <= 0) {
            yield break;
        }

        while (count > 0) {
            var maxWorkersOnMineral = _minerals.Max(mineral => UnitModule.Get<CapacityModule>(mineral).AssignedUnits.Count);
            if (maxWorkersOnMineral == 0) {
                Logger.Error("Trying to release mining workers, but there's no mining workers");
                break;
            }

            var capacityModulesToPickFrom = _minerals.Select(UnitModule.Get<CapacityModule>)
                .Where(capacityModule => capacityModule.AssignedUnits.Count == maxWorkersOnMineral)
                .Take(count);

            foreach (var capacityModuleToPickFrom in capacityModulesToPickFrom) {
                var workerToRelease = capacityModuleToPickFrom.ReleaseOne();
                if (workerToRelease != null) {
                    ReleaseWorker(workerToRelease);
                    yield return workerToRelease;
                    count--;
                }
            }
        }
    }
}
