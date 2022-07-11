using System.Linq;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class MineralMiningModule: IUnitModule {
    private readonly Unit _worker;
    private readonly Unit _targetMineral;

    public MineralMiningModule(Unit worker, Unit targetMineral) {
        _worker = worker;
        _targetMineral = targetMineral;
    }

    public void Execute() {
        // Make sure each worker gathers from its target patch
        // TODO GD Fast mining consists of moving to the base, not using return cargo
        if (_worker.Orders.Count == 0 || _worker.Orders.Count == 1 && _worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != _targetMineral.Tag)) {
            _worker.Gather(_targetMineral);
        }

        Debugger.AddLine(_worker.Position, _targetMineral.Position, Colors.Magenta);
    }
}
