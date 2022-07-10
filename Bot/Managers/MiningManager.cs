using System.Collections.Generic;
using System.Linq;
using Bot.UnitModules;
using SC2APIProtocol;

namespace Bot.Managers;

public class MiningManager: IManager {
    private const int MaxGas = 2;
    private const int MaxExtractorsPerGas = 1;
    private const int MaxPerExtractor = 3;
    private const int MaxMinerals = 8;
    private const int IdealPerMinerals = 2;
    private const int MaxPerMinerals = 2;
    private const int MaxDistanceToExpand = 10;

    private readonly Unit _base;
    private readonly Color _color;

    private readonly List<Unit> _workers = new List<Unit>();
    private readonly List<Unit> _extractors = new List<Unit>();
    private readonly List<Unit> _minerals;
    private readonly List<Unit> _gasses;

    private int _mineralRoundRobinIndex = 0;

    private string MiningModuleTag => $"{_base.Tag}-mining";

    private string DebugLocationModuleTag => $"{_base.Tag}-debug-location";

    public int IdealAvailableCapacity => _minerals.Count * IdealPerMinerals + _extractors.Count(extractor => extractor.IsOperational) * MaxPerExtractor - _workers.Count;
    public int SaturatedAvailableCapacity => IdealAvailableCapacity + _minerals.Count; // Can allow 1 more per mineral patch

    public MiningManager(Unit hatchery, Color color) {
        _base = hatchery;
        _color = color;

        _base.Modules.Add(DebugLocationModuleTag, new DebugLocationModule(_base, _color));
        _base.Modules[DebugLocationModuleTag].Execute();

        _minerals = Controller.GetUnits(Controller.NeutralUnits, Units.MineralFields)
            .Where(mineral => mineral.DistanceTo(_base) < MaxDistanceToExpand)
            .Where(mineral => !UnitUtils.IsResourceManaged(mineral))
            .Take(MaxMinerals)
            .ToList();

        _minerals.ForEach(mineral => {
            mineral.Modules.Add(CapacityModule.GlobalTag, new CapacityModule(MaxPerMinerals)); // TODO GD Use the module to equalize workers
            mineral.Modules.Add(DebugLocationModuleTag, new DebugLocationModule(mineral, _color));
        });

        _gasses = Controller.GetUnits(Controller.NeutralUnits, Units.GasGeysers)
            .Where(gas => gas.DistanceTo(_base) < MaxDistanceToExpand)
            .Where(gas => !UnitUtils.IsResourceManaged(gas))
            .Take(MaxGas)
            .ToList();

        _gasses.ForEach(gas => {
            gas.Modules.Add(CapacityModule.GlobalTag, new CapacityModule(MaxExtractorsPerGas));
            gas.Modules.Add(DebugLocationModuleTag, new DebugLocationModule(gas, _color));
        });
    }

    public void AssignWorkers(List<Unit> workers) {
        workers.ForEach(worker => {
            worker.AddDeathWatcher(this);
            worker.Modules.Add(DebugLocationModuleTag, new DebugLocationModule(worker, _color));
        });
        _workers.AddRange(workers);

        var dispatched = 0;

        // Assign workers to gas
        _extractors.Where(extractor => extractor.IsOperational).ToList().ForEach(extractor => {
            var availableCapacity = CapacityModule.GetAvailableCapacity(extractor);

            var assignedWorkers = workers.Skip(dispatched).Take(availableCapacity).ToList();

            CapacityModule.Assign(extractor, assignedWorkers);
            assignedWorkers.ForEach(worker => worker.Modules.Add(MiningModuleTag, new GasMiningModule(worker, extractor)));

            dispatched += availableCapacity;
        });

        // Assign workers to minerals
        workers.Skip(dispatched).ToList().ForEach(worker => {
            worker.Modules.Add(MiningModuleTag, new MineralMiningModule(worker, _minerals[_mineralRoundRobinIndex]));
            _mineralRoundRobinIndex = (_mineralRoundRobinIndex + 1) % _minerals.Count;
        });

        // Do something with leftovers
    }

    public void OnFrame() {
        // Get new extractors
        if (_extractors.Count < _gasses.Count) {
            var newExtractors = Controller.GetUnits(Controller.NewOwnedUnits, Units.Extractor)
                .Where(extractor => extractor.DistanceTo(_base) < MaxDistanceToExpand)
                .Take(MaxGas - _extractors.Count)
                .ToList();

            newExtractors.ForEach(newExtractor => {
                newExtractor.AddDeathWatcher(this);
                newExtractor.Modules.Add(CapacityModule.GlobalTag, new CapacityModule(MaxPerExtractor));
                newExtractor.Modules.Add(DebugLocationModuleTag, new DebugLocationModule(newExtractor, _color));
                CapacityModule.Assign(_gasses.First(gas => gas.DistanceTo(newExtractor) < 1), newExtractor);
            });

            _extractors.AddRange(newExtractors);
        }

        // Get to work!
        _workers.ForEach(worker => {
            worker.Modules[MiningModuleTag].Execute();
            worker.Modules[DebugLocationModuleTag].Execute();
        });

        _base.Modules[DebugLocationModuleTag].Execute();
        _minerals.ForEach(mineral => mineral.Modules[DebugLocationModuleTag].Execute());
        _gasses.ForEach(gas => gas.Modules[DebugLocationModuleTag].Execute());
        _extractors.ForEach(extractor => extractor.Modules[DebugLocationModuleTag].Execute());
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (deadUnit.UnitType == Units.Drone) {
            _workers.Remove(deadUnit);
        }
        else if (Units.MineralFields.Contains(deadUnit.UnitType)) {
            _minerals.Remove(deadUnit);
        }
        else if (Units.GasGeysers.Contains(deadUnit.UnitType)) {
            _gasses.Remove(deadUnit);
        }
        else if (deadUnit.UnitType == Units.Extractor) {
            _extractors.Remove(deadUnit);
        }
    }
}
