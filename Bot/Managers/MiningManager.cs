using System;
using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.UnitModules;
using SC2APIProtocol;

namespace Bot.Managers;

public class MiningManager: IManager {
    private const int MaxGas = 2;
    private const int MaxExtractorsPerGas = 1;
    private const int MaxPerExtractor = 3;
    private const int MaxMinerals = 8;
    private const int IdealPerMinerals = 2;
    private const int MaxPerMinerals = 3;
    private const int MaxDistanceToExpand = 10;

    public readonly Unit TownHall;
    public Unit Queen;
    private readonly Color _color;

    private readonly List<Unit> _workers = new List<Unit>();
    private readonly List<Unit> _extractors = new List<Unit>();
    private readonly List<Unit> _minerals;
    private readonly List<Unit> _gasses;

    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep> { new BuildOrders.BuildStep(BuildType.Train, 0, Units.Drone, 0) };
    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests;

    public int IdealAvailableCapacity => _minerals.Count * IdealPerMinerals + _extractors.Count(extractor => extractor.IsOperational) * MaxPerExtractor - _workers.Count;
    public int SaturatedAvailableCapacity => IdealAvailableCapacity + _minerals.Count; // Can allow 1 more per mineral patch

    public MiningManager(Unit townHall, Color color) {
        TownHall = townHall;
        _color = color;

        DebugLocationModule.Install(TownHall, _color);

        _minerals = Controller.GetUnits(Controller.NeutralUnits, Units.MineralFields)
            .Where(mineral => mineral.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Where(mineral => !UnitUtils.IsResourceManaged(mineral))
            .Take(MaxMinerals)
            .ToList();

        _minerals.ForEach(mineral => {
            mineral.AddDeathWatcher(this);

            DebugLocationModule.Install(mineral, _color);
            CapacityModule.Install(mineral, MaxPerMinerals);
        });

        _gasses = Controller.GetUnits(Controller.NeutralUnits, Units.GasGeysers)
            .Where(gas => gas.DistanceTo(TownHall) < MaxDistanceToExpand)
            .Where(gas => !UnitUtils.IsResourceManaged(gas))
            .Where(gas => !IsGasDepleted(gas))
            .Take(MaxGas)
            .ToList();

        _gasses.ForEach(gas => {
            DebugLocationModule.Install(gas, _color);
            CapacityModule.Install(gas, MaxExtractorsPerGas);
        });

        DiscoverExtractors(Controller.OwnedUnits);
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
            worker.AddDeathWatcher(this);
            DebugLocationModule.Install(worker, _color);
        }

        DispatchWorkers(workers);
    }

    public void OnFrame() {
        // TODO GD They don't count the drone eggs, so they'll always request more drones than needed
        _buildStepRequests[0].Quantity = (uint)Math.Max(0, SaturatedAvailableCapacity);

        HandleDepletedGasses();
        DiscoverExtractors(Controller.NewOwnedUnits);

        DispatchWorkers(GetIdleWorkers());

        if (_minerals.Sum(mineral => UnitModule.Get<CapacityModule>(mineral).AvailableCapacity) <= _minerals.Count) {
            FillExtractors();
        }
    }

    public void Retire() {
        UnitModule.Uninstall<DebugLocationModule>(TownHall);

        _workers.ForEach(worker => {
            worker.RemoveDeathWatcher(this);
            UnitModule.Uninstall<DebugLocationModule>(worker);
            UnitModule.Uninstall<MiningModule>(worker);
        });

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
            Queen.RemoveDeathWatcher(this);
            UnitModule.Uninstall<DebugLocationModule>(Queen);
            UnitModule.Uninstall<QueenMicroModule>(Queen);
        }
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
            // TODO GD Select from busiest minerals instead
            var workersToReassign = _workers
                .Where(worker => UnitModule.Get<MiningModule>(worker).ResourceType == UnitUtils.ResourceType.Mineral)
                .Take(UnitModule.Get<CapacityModule>(extractor).AvailableCapacity)
                .ToList();

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
            .OrderBy(extractor => extractor.DistanceTo(worker))
            .FirstOrDefault();
    }

    private Unit GetClosestMineralWithAvailableCapacity(Unit worker, int minAvailableCapacity) {
        return _minerals
            .Where(mineral => UnitModule.Get<CapacityModule>(mineral).AvailableCapacity > minAvailableCapacity)
            .OrderBy(mineral => mineral.DistanceTo(worker))
            .FirstOrDefault();
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
        deadExtractor.RemoveDeathWatcher(this);

        var capacityModule = UnitModule.Uninstall<CapacityModule>(deadExtractor);
        if (capacityModule != null) {
            capacityModule.AssignedUnits.ForEach(worker => UnitModule.Uninstall<MiningModule>(worker));
        }
    }
}
