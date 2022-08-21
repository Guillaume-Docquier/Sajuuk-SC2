using System;
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
public class TownHallManager: IManager {
    private const int MaxGas = 2;
    private const int MaxExtractorsPerGas = 1;
    private const int MaxPerExtractor = 3;
    private const int MaxMinerals = 8;
    private const int IdealPerMinerals = 2;
    private const int MaxPerMinerals = 3;
    private const int MaxDistanceToExpand = 10;

    private bool _expandHasBeenRequested = false;

    public readonly Unit TownHall;
    public Unit Queen;
    private readonly Color _color;

    private readonly List<Unit> _workers = new List<Unit>();
    private readonly List<Unit> _extractors = new List<Unit>();
    private readonly List<Unit> _minerals;
    private readonly List<Unit> _gasses;

    private readonly BuildRequest _expandBuildRequest = new QuantityBuildRequest(BuildType.Expand, Units.Hatchery, atSupply: 75, quantity: 0);
    private readonly List<BuildRequest> _buildStepRequests = new List<BuildRequest>();

    public IEnumerable<BuildFulfillment> BuildFulfillments => _buildStepRequests.Select(buildRequest => buildRequest.Fulfillment);

    public int IdealCapacity => _minerals.Count * IdealPerMinerals + _extractors.Count(extractor => extractor.IsOperational) * MaxPerExtractor;
    public int IdealAvailableCapacity => IdealCapacity - _workers.Count;

    public int SaturatedCapacity => IdealCapacity + _minerals.Count; // Can allow 1 more per mineral patch;
    public int SaturatedAvailableCapacity => SaturatedCapacity - _workers.Count;

    public int WorkerCount => _workers.Count;

    public TownHallManager(Unit townHall, Color color) {
        TownHall = townHall;
        _color = color;

        DebugLocationModule.Install(TownHall, _color);

        _minerals = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralFields)
            .Where(mineral => mineral.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Where(mineral => mineral.Supervisor == null)
            .Take(MaxMinerals)
            .ToList();

        _minerals.ForEach(mineral => {
            mineral.AddDeathWatcher(this);
            mineral.Supervisor = this;

            DebugLocationModule.Install(mineral, _color);
            CapacityModule.Install(mineral, MaxPerMinerals);
        });

        _gasses = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.GasGeysers)
            .Where(gas => gas.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Where(gas => gas.Supervisor == null)
            .Where(gas => !IsGasDepleted(gas))
            .Take(MaxGas)
            .ToList();

        _gasses.ForEach(gas => {
            gas.Supervisor = this;

            DebugLocationModule.Install(gas, _color);
            CapacityModule.Install(gas, MaxExtractorsPerGas);
        });

        DiscoverExtractors(UnitsTracker.OwnedUnits);

        _buildStepRequests.Add(_expandBuildRequest);

        // You're a macro hatch
        if (_minerals.Count == 0) {
            _expandHasBeenRequested = true;
        }
    }

    public void AssignQueen(Unit queen) {
        Queen = queen;

        Queen.AddDeathWatcher(this);
        DebugLocationModule.Install(Queen, _color);
        QueenMicroModule.Install(Queen, TownHall);
    }

    public void AssignWorker(Unit worker) {
        AssignWorkers(new List<Unit> { worker });
    }

    public void AssignWorkers(List<Unit> workers) {
        _workers.AddRange(workers);

        foreach (var worker in workers) {
            worker.Supervisor = this;
            worker.AddDeathWatcher(this);
            DebugLocationModule.Install(worker, _color);
        }

        DispatchWorkers(workers);
    }

    public IEnumerable<Unit> ReleaseWorkers(int count) {
        foreach (var idleWorker in GetIdleWorkers().Take(count).ToList()) {
            yield return ReleaseWorker(idleWorker);
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
                yield return ReleaseWorker(capacityModuleToPickFrom.ReleaseOne());
                count--;
            }
        }
    }

    public void OnFrame() {
        DrawTownHallInfo();

        HandleDepletedGasses();
        DiscoverExtractors(UnitsTracker.NewOwnedUnits);

        DispatchWorkers(GetIdleWorkers());

        if (_minerals.Sum(mineral => UnitModule.Get<CapacityModule>(mineral).AvailableCapacity) <= _minerals.Count) {
            FillExtractors();
        }

        RequestExpand();
    }

    public void Release(Unit unit) {
        if (unit == null) {
            return;
        }

        if (Queen == unit) {
            ReleaseQueen();
        }

        if (_workers.Contains(unit)) {
            ReleaseWorker(unit);
        }
    }

    public void Retire() {
        UnitModule.Uninstall<DebugLocationModule>(TownHall);

        _workers.ToList().ForEach(worker => ReleaseWorker(worker));

        _minerals.ForEach(mineral => {
            mineral.RemoveDeathWatcher(this);
            UnitModule.Uninstall<DebugLocationModule>(mineral);
            UnitModule.Uninstall<CapacityModule>(mineral);
        });

        _gasses.ForEach(gas => {
            gas.RemoveDeathWatcher(this);
            UnitModule.Uninstall<DebugLocationModule>(gas);
            UnitModule.Uninstall<CapacityModule>(gas);
        });

        _extractors.ForEach(extractor => {
            extractor.RemoveDeathWatcher(this);
            UnitModule.Uninstall<DebugLocationModule>(extractor);
            UnitModule.Uninstall<CapacityModule>(extractor);
        });

        if (Queen != null) {
            ReleaseQueen();
        }
    }

    private Unit ReleaseWorker(Unit worker) {
        if (worker == null) {
            Logger.Error("Trying to release a null worker");
            return null;
        }

        worker.RemoveDeathWatcher(this);
        UnitModule.Uninstall<DebugLocationModule>(worker);
        UnitModule.Uninstall<MiningModule>(worker);
        _workers.Remove(worker);

        return worker;
    }

    private void ReleaseQueen() {
        Queen.RemoveDeathWatcher(this);
        UnitModule.Uninstall<DebugLocationModule>(Queen);
        UnitModule.Uninstall<QueenMicroModule>(Queen);
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (deadUnit.UnitType == Units.Drone) {
            _workers.Remove(deadUnit);
        }
        else if (Units.MineralFields.Contains(deadUnit.UnitType)) {
            _minerals.Remove(deadUnit);
            UnitModule.Get<CapacityModule>(deadUnit).AssignedUnits.ForEach(worker => UnitModule.Uninstall<MiningModule>(worker));
        }
        else if (deadUnit.UnitType == Units.Extractor) {
            HandleDeadExtractor(deadUnit);
        }
        else if (deadUnit.UnitType == Units.Queen) {
            Queen = null;
        }
    }

    private IEnumerable<Unit> GetIdleWorkers() {
        return _workers.Where(worker => UnitModule.Get<MiningModule>(worker) == null);
    }

    private void DispatchWorkers(IEnumerable<Unit> workers) {
        foreach (var worker in workers) {
            var assignedResource = GetClosestExtractorWithAvailableCapacity(worker);
            assignedResource ??= GetClosestMineralWithAvailableCapacity(worker, minAvailableCapacity: 1);
            assignedResource ??= GetClosestMineralWithAvailableCapacity(worker, minAvailableCapacity: 0);
            assignedResource ??= GetClosestMineralWithAvailableCapacity(worker, minAvailableCapacity: -999);

            if (assignedResource != null) {
                MiningModule.Install(worker, assignedResource);
                UnitModule.Get<CapacityModule>(assignedResource).Assign(worker);
            }
        }
    }

    private void FillExtractors() {
        foreach (var extractor in _extractors.Where(extractor => extractor.IsOperational)) {
            var workersToReassign = _workers
                .Where(worker => UnitModule.Get<MiningModule>(worker).ResourceType == UnitUtils.ResourceType.Mineral)
                .OrderBy(worker => {
                    var resource = UnitModule.Get<MiningModule>(worker).AssignedResource;

                    return UnitModule.Get<CapacityModule>(resource).AvailableCapacity;
                })
                .Take(UnitModule.Get<CapacityModule>(extractor).AvailableCapacity);

            foreach (var worker in workersToReassign) {
                UpdateWorkerAssignment(worker, extractor);
            }
        }
    }

    private void DiscoverExtractors(IEnumerable<Unit> newUnits) {
        if (_extractors.Count >= _gasses.Count) {
            return;
        }

        var newExtractors = Controller.GetUnits(newUnits, Units.Extractor)
            .Where(extractor => _gasses.Any(gas => extractor.DistanceTo(gas) < 1)) // Should be 0, we chose 1 just in case
            .Where(extractor => !_extractors.Contains(extractor)) // Safety check
            .ToList();

        ManageExtractors(newExtractors);
    }

    private void ManageExtractors(List<Unit> extractors) {
        foreach (var extractor in extractors) {
            extractor.AddDeathWatcher(this);

            DebugLocationModule.Install(extractor, _color);
            CapacityModule.Install(extractor, MaxPerExtractor);
            UnitModule.Get<CapacityModule>(_gasses.First(gas => gas.DistanceTo(extractor) < 1)).Assign(extractor);
        }

        _extractors.AddRange(extractors);
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

    private void UpdateWorkerAssignment(Unit worker, Unit assignedResource) {
        var oldMiningModule = UnitModule.Uninstall<MiningModule>(worker);
        UnitModule.Get<CapacityModule>(oldMiningModule.AssignedResource).Release(worker);

        MiningModule.Install(worker, assignedResource);
        UnitModule.Get<CapacityModule>(assignedResource).Assign(worker);
    }

    private bool IsGasDepleted(Unit gas) {
        return gas.RawUnitData.DisplayType != DisplayType.Snapshot && gas.RawUnitData.VespeneContents <= 0;
    }

    private void HandleDepletedGasses() {
        foreach (var depletedGas in _gasses.Where(IsGasDepleted).ToList()) {
            _gasses.Remove(depletedGas);
            UnitModule.Uninstall<DebugLocationModule>(depletedGas);

            var uselessExtractor = UnitModule.Get<CapacityModule>(depletedGas).AssignedUnits.FirstOrDefault();
            if (uselessExtractor != null) {
                uselessExtractor.RemoveDeathWatcher(this);
                UnitModule.Uninstall<DebugLocationModule>(uselessExtractor);

                HandleDeadExtractor(uselessExtractor);
            }
        }
    }

    private void HandleDeadExtractor(Unit deadExtractor) {
        _extractors.Remove(deadExtractor);

        var capacityModule = UnitModule.Uninstall<CapacityModule>(deadExtractor);
        if (capacityModule != null) {
            capacityModule.AssignedUnits.ForEach(worker => UnitModule.Uninstall<MiningModule>(worker));
        }
    }

    private void RequestExpand() {
        // TODO GD Use a target build order to keep mining gas
        // TODO GD This doesn't count depleted minerals
        // TODO GD Should probably put this in expand analyzer, but it's fine for now
        if (TownHall.IsOperational && !_expandHasBeenRequested && (GetMineralsPercent() <= 0.6 || _minerals.Count <= 5)) {
            _expandBuildRequest.Requested += 1;

            _expandHasBeenRequested = true;
        }
    }

    private void DrawTownHallInfo() {
        GraphicalDebugger.AddTextGroup(new[]
            {
                $"IdealAvailableCapacity: {IdealAvailableCapacity}",
                $"SaturatedAvailableCapacity: {SaturatedAvailableCapacity}",
                $"MineralsPercent: {GetMineralsPercent():P}"
            },
            worldPos: TownHall.Position.Translate(xTranslation: -2.5f, yTranslation: 1f).ToPoint());
    }

    private float GetMineralsPercent() {
        if (_minerals.Count == 0) {
            return 0;
        }

        return (float)_minerals.Sum(mineral => mineral.RawUnitData.MineralContents) / _minerals.Sum(mineral => mineral.InitialMineralCount);
    }
}
