using System.Collections.Generic;
using System.Linq;
using static System.String;

namespace Bot.Managers;

public class MiningManager: IManager {
    private const int MaxGas = 2;
    private const int MaxPerGas = 3;
    private const int MaxMinerals = 8;
    private const int MaxPerMinerals = 2;

    private readonly Unit _base;

    private readonly List<Unit> _minerals;
    private readonly Dictionary<Unit, List<Unit>> _mineralDispatch;

    private readonly List<Unit> _gasses;
    private readonly Dictionary<Unit, List<Unit>> _gasDispatch;

    private List<Unit> _workers = new List<Unit>();

    // TODO GD This doesn't count gasses
    public int IdealAvailableCapacity => _base.IdealWorkerCount - _workers.Count;
    public int SaturatedAvailableCapacity => _base.SaturatedWorkerCount - _workers.Count;

    public MiningManager(Unit hatchery) {
        _base = hatchery;

        // TODO GD Maybe check that they're not already managed?
        _minerals = Controller.GetUnits(Controller.NeutralUnits, Units.MineralFields)
            .Where(mineral => mineral.GetDistance(_base) < 10)
            .Take(MaxMinerals)
            .ToList();

        _mineralDispatch = _minerals.ToDictionary(mineral => mineral, _ => new List<Unit>());

        _gasses = Controller.GetUnits(Controller.NeutralUnits, Units.GasGeysers)
            .Where(gas => gas.GetDistance(_base) < 10)
            .Take(MaxGas)
            .ToList();

        _gasDispatch = _gasses.ToDictionary(gas => gas, _ => new List<Unit>());
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

        // TODO GD This is overly complicated and not efficient, replace this with a round robin and put target data in units
        // Assign workers to minerals
        var mineralIndex = 0;
        var mineralDispatch = _mineralDispatch[_minerals[mineralIndex]];
        while (workerIndex < workers.Count && mineralIndex < _minerals.Count - 1) {
            while (mineralDispatch.Count >= 2 && mineralIndex < _minerals.Count - 1) {
                mineralIndex++;
                mineralDispatch = _mineralDispatch[_minerals[mineralIndex]];
            }

            if (mineralIndex < _minerals.Count - 1) {
                mineralDispatch.Add(workers[workerIndex]);
                workerIndex++;
            }
        }

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

        foreach (var mineralDispatch in _mineralDispatch) {
            mineralDispatch.Value.ForEach(worker => {
                // TODO GD Fast mining consists of moving to the base, not using return cargo
                if (worker.Orders.Count == 0 || worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != mineralDispatch.Key.Tag)) {
                    worker.Gather(mineralDispatch.Key);
                }
            });
        }
    }
}
