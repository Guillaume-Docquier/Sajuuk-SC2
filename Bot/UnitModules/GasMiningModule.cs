using System.Linq;

namespace Bot.UnitModules;

public class GasMiningModule: IUnitModule {
    private readonly Unit _worker;
    private readonly Unit _targetGasExtractor;

    public GasMiningModule(Unit worker, Unit targetGasExtractor) {
        _worker = worker;
        _targetGasExtractor = targetGasExtractor;
    }

    public void Execute() {
        if (_worker.Orders.Count == 0 || _worker.Orders.Count == 1 && _worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != _targetGasExtractor.Tag)) {
            _worker.Gather(_targetGasExtractor);
        }
    }
}
