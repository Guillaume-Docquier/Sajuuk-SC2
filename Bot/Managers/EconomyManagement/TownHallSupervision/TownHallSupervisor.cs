using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;
using SC2APIProtocol;

namespace Bot.Managers.EconomyManagement.TownHallSupervision;

public partial class TownHallSupervisor: Supervisor, IWatchUnitsDie {
    private readonly ulong _id;
    private readonly Color _color;
    public Unit TownHall { get; private set; }
    public Unit Queen { get; private set; }

    private readonly HashSet<Unit> _minerals = new HashSet<Unit>();
    private readonly HashSet<Unit> _gasses = new HashSet<Unit>();
    private readonly HashSet<Unit> _extractors = new HashSet<Unit>();
    private readonly HashSet<Unit> _workers = new HashSet<Unit>();

    private bool _expandHasBeenRequested = false;
    private readonly int _initialMineralsSum;

    // TODO GD atSupply for expands depends on the build order, maybe only after the BO is finished instead?
    private readonly BuildRequest _expandBuildRequest = new QuantityBuildRequest(BuildType.Expand, Units.Hatchery, atSupply: 75, quantity: 0, blockCondition: BuildBlockCondition.MissingResources, priority: BuildRequestPriority.High);
    private readonly List<BuildRequest> _buildStepRequests = new List<BuildRequest>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildStepRequests.Select(buildRequest => buildRequest.Fulfillment);

    protected override IAssigner Assigner { get; }
    protected override IReleaser Releaser { get; }

    public int IdealCapacity => _minerals.Count * Resources.IdealDronesPerMinerals + _extractors.Count(extractor => extractor.IsOperational) * Resources.MaxDronesPerExtractor;
    public int IdealAvailableCapacity => IdealCapacity - _workers.Count;

    public int SaturatedCapacity => IdealCapacity + _minerals.Count; // Can allow 1 more per mineral patch;
    public int SaturatedAvailableCapacity => SaturatedCapacity - _workers.Count;

    public int WorkerCount => _workers.Count;

    // TODO GD Checking that the extractor is operational is annoying
    public int MaxGasCapacity => Math.Min(WorkerCount, _extractors.Count(extractor => extractor.IsOperational) * Resources.MaxDronesPerExtractor);

    private int _gasWorkersCap = 0;
    public int GasWorkersCap {
        get => _gasWorkersCap;
        set {
            if (value < 0) {
                Logger.Error($"Trying to set a negative GasWorkersCap: {value}\n");
                _gasWorkersCap = 0;
            }

            _gasWorkersCap = value;
        }
    }

    public TownHallSupervisor(Unit townHall, Color color) {
        _id = townHall.Tag;
        _color = color;

        Assigner = new TownHallSupervisorAssigner(this);
        Releaser = new TownHallSupervisorReleaser(this);

        Assign(townHall);
        Assign(DiscoverMinerals());
        Assign(DiscoverGasses());
        Assign(DiscoverExtractors(UnitsTracker.OwnedUnits));

        _buildStepRequests.Add(_expandBuildRequest);

        // You're a macro hatch
        if (_minerals.Count == 0) {
            _expandHasBeenRequested = true;
        }

        // TODO GD Put this in Expand analyzer, and try to find max minerals based on patch type?
        _initialMineralsSum = _minerals.Sum(mineral => mineral.InitialMineralCount);
    }

    protected override void Supervise() {
        DrawTownHallInfo();

        if (Controller.Frame == 0) {
            SplitInitialWorkers();
            return;
        }

        ReleaseDepletedGasses();
        Assign(DiscoverExtractors(UnitsTracker.NewOwnedUnits));

        UpdateExtractorCapacities();
        UpdateGasAssignments();

        DispatchWorkers(GetIdleWorkers());

        RequestExpandIfNeeded();
    }

    public override void Retire() {
        if (TownHall != null) {
            Release(TownHall);
        }

        if (Queen != null) {
            Release(Queen);
        }

        foreach (var mineral in _minerals) Release(mineral);
        foreach (var gas in _gasses) Release(gas);

        foreach (var worker in _workers) Release(worker);
        foreach (var extractor in _extractors) Release(extractor);

        Logger.Debug("({0}) Retired", this);
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (Units.MineralFields.Contains(deadUnit.UnitType)) {
            Release(deadUnit);
        }
        else if (deadUnit.UnitType == Units.Extractor) {
            Release(deadUnit);
        }
        else {
            Logger.Error("({0}) Reported death of {1}, but we don't death watch this unit type", this, deadUnit);
        }
    }

    private List<Unit> GetIdleWorkers() {
        return _workers.Where(worker => UnitModule.Get<MiningModule>(worker).AssignedResource == null).ToList();
    }

    /// <summary>
    /// <para>
    /// Splits the workers at the start of the game.<br/>
    /// We allow a maximum of two workers per patch.
    /// </para>
    ///
    /// <para>
    /// The idea is to assign the worker that's the farthest from any mineral first, to its closest mineral.<br/>
    /// This way, outside workers are not stuck with terrible minerals choices.<br/>
    /// Workers in the center have many close minerals so we can afford to choose one that's slightly farther.
    /// </para>
    ///
    /// <para>
    /// This code is not optimal, but reuses existing code.<br/>
    /// On frame 0 we have nothing else to do anyways.
    /// </para>
    /// </summary>
    private void SplitInitialWorkers() {
        var workers = GetIdleWorkers().ToHashSet();
        while (workers.Count > 0) {
            var farthestWorker = workers.MaxBy(
                worker => GetClosestMineralWithAvailableCapacity(worker, 1).DistanceTo(worker)
            );
            var closestMineral = GetClosestMineralWithAvailableCapacity(farthestWorker, 1);

            UpdateWorkerMiningAssignment(farthestWorker, closestMineral);
            workers.Remove(farthestWorker);
        }
    }

    /// <summary>
    /// Updates the capacity of the extractors to match the gas workers cap.
    /// </summary>
    private void UpdateExtractorCapacities() {
        var availableCapacity = GasWorkersCap;
        foreach (var extractor in _extractors.Where(extractor => extractor.IsOperational)) {
            var capacityModule = UnitModule.Get<CapacityModule>(extractor);
            var newExtractorCapacity = Math.Min(Resources.MaxDronesPerExtractor, availableCapacity);

            capacityModule.MaxCapacity = newExtractorCapacity;
            availableCapacity -= newExtractorCapacity;
        }
    }

    /// <summary>
    /// Update the drone assignments to satisfy gas requirements.
    /// </summary>
    private void UpdateGasAssignments() {
        foreach (var extractor in _extractors.Where(extractor => extractor.IsOperational)) {
            var extractorCapacityModule = UnitModule.Get<CapacityModule>(extractor);

            if (extractorCapacityModule.AvailableCapacity > 0) {
                FillExtractor(extractor);
            }
            else {
                while (extractorCapacityModule.AvailableCapacity < 0) {
                    var releasedWorker = extractorCapacityModule.ReleaseOne();
                    UpdateWorkerMiningAssignment(releasedWorker, null);
                }
            }
        }
    }

    /// <summary>
    /// Take drones off of the mineral line and assign them to the given extractor
    /// </summary>
    private void FillExtractor(Unit extractor) {
        var workersToReassign = _workers
            .Where(worker => UnitModule.Get<MiningModule>(worker).ResourceType == Resources.ResourceType.Mineral)
            .OrderBy(worker => {
                var resource = UnitModule.Get<MiningModule>(worker).AssignedResource;

                return UnitModule.Get<CapacityModule>(resource).AvailableCapacity;
            })
            .Take(UnitModule.Get<CapacityModule>(extractor).AvailableCapacity);

        foreach (var worker in workersToReassign) {
            UpdateWorkerMiningAssignment(worker, extractor);
        }
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

        if (assignedResource != null) {
            UpdateWorkerMiningAssignment(worker, assignedResource);
        }
        else {
            Release(worker);
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

    /// <summary>
    /// Assigns a worker to a resource and releases the worker from its previously assigned resource, if any.
    /// </summary>
    /// <param name="worker">The worker to update the mining assignment of.</param>
    /// <param name="newlyAssignedResource">The new resource to mine, or null to idle a worker</param>
    private static void UpdateWorkerMiningAssignment(Unit worker, Unit newlyAssignedResource) {
        if (worker == null) {
            throw new ArgumentNullException(nameof(worker));
        }

        UnitModule.Get<MiningModule>(worker).AssignResource(newlyAssignedResource, releasePreviouslyAssignedResource: true);
    }

    private void ReleaseDepletedGasses() {
        foreach (var depletedGas in _gasses.Where(IsGasDepleted)) {
            Release(depletedGas);
        }
    }

    private static bool IsGasDepleted(Unit gas) {
        return gas.RawUnitData.DisplayType != DisplayType.Snapshot && gas.RawUnitData.VespeneContents <= 0;
    }

    /// <summary>
    /// Requests to build an expand once the remaining minerals reaches a certain threshold.
    /// We can only request an expand if the TownHall is operational and we request a maximum of 1 expand.
    /// </summary>
    private void RequestExpandIfNeeded() {
        if (!TownHall.IsOperational || _expandHasBeenRequested) {
            return;
        }

        // We don't always instantly see the minerals and snapshot units have no contents
        if (_minerals.Any(mineral => mineral.RawUnitData.DisplayType != DisplayType.Visible)) {
            return;
        }

        // TODO GD This doesn't count minerals if they were depleted when we built the hatch
        // TODO GD Should probably put this in expand analyzer, but it's fine for now
        if (GetRemainingMineralsPercent() <= 0.6 || _minerals.Count <= 5) {
            Logger.Info("(TownHallManager) Running low on resources ({0:P2} / {1} minerals), requesting expand", GetRemainingMineralsPercent(), _minerals.Count);
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

        Program.GraphicalDebugger.AddTextGroup(new[]
            {
                $"IdealAvailCapacity: {IdealAvailableCapacity}",
                $"SaturAvailCapacity: {SaturatedAvailableCapacity}",
                $"Minerals: {GetRemainingMineralsPercent():P}",
                $"Gas: TODO",
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
        foreach (var idleWorker in GetIdleWorkers().Take(count)) {
            Release(idleWorker);
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
                    Release(workerToRelease);
                    yield return workerToRelease;
                    count--;
                }
            }
        }
    }

    public override string ToString() {
        return $"TownHallSupervisor[{_id}]";
    }
}
