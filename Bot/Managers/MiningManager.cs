using System.Collections.Generic;
using System.Linq;
using Bot.UnitModules;

namespace Bot.Managers;

public class MiningManager: IManager {
    private const int MaxGas = 2;
    private const int MaxPerGas = 3;
    private const int MaxMinerals = 8;
    private const int IdealPerMinerals = 2;
    private const int MaxPerMinerals = 3;

    private readonly Unit _base;

    private readonly List<Unit> _workers = new List<Unit>();
    private readonly List<Unit> _minerals;
    private readonly List<Unit> _gasses;

    private int _mineralRoundRobinIndex = 0;

    // TODO GD This doesn't count gasses
    public int IdealAvailableCapacity => _minerals.Count * IdealPerMinerals - _workers.Count; // TODO GD Include gas
    public int SaturatedAvailableCapacity => _minerals.Count * MaxPerMinerals - _workers.Count; // TODO GD Include gas

    public MiningManager(Unit hatchery) {
        _base = hatchery;

        // TODO GD Maybe check that they're not already managed?
        _minerals = Controller.GetUnits(Controller.NeutralUnits, Units.MineralFields)
            .Where(mineral => mineral.GetDistance(_base) < 10)
            .Take(MaxMinerals)
            .ToList();

        _gasses = Controller.GetUnits(Controller.NeutralUnits, Units.GasGeysers)
            .Where(gas => gas.GetDistance(_base) < 10)
            .Take(MaxGas)
            .ToList();
    }

    public void AssignWorkers(List<Unit> workers) {
        _workers.AddRange(workers);

        var workerIndex = 0;

        // TODO GD Check for extractors
        // TODO GD Make this work
        // Assign workers to gas
        // var gasIndex = 0;
        // var gasDispatch = _gasDispatch[_gasses[gasIndex]];
        // while (workerIndex < workers.Count && gasIndex < _gasses.Count - 1) {
        //     while (gasDispatch.Count >= 2 && gasIndex < _gasses.Count - 1) {
        //         gasIndex++;
        //         gasDispatch = _gasDispatch[_gasses[gasIndex]];
        //     }
//
        //     if (gasIndex < _gasDispatch.Count - 1) {
        //         gasDispatch.Add(workers[workerIndex]);
        //         workerIndex++;
        //     }
        // }

        // Assign workers to minerals
        workers.ForEach(worker => {
            worker.Modules.Add(_base.Tag, new MineralMiningModule(worker, _minerals[_mineralRoundRobinIndex]));
            _mineralRoundRobinIndex = (_mineralRoundRobinIndex + 1) % _minerals.Count;
        });

        // Do something with leftovers
    }

    public void OnFrame() {
        // TODO GD Make this work
        //foreach (var gasDispatch in _gasDispatch) {
        //    gasDispatch.Value.ForEach(worker => {
        //        if (worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != gasDispatch.Key.Tag)) {
        //            worker.Gather(gasDispatch.Key);
        //        }
        //    });
        //}

        _workers.ForEach(worker => worker.Modules[_base.Tag].Execute());
    }
}
