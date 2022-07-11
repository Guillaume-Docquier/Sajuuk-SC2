using System;
using System.Linq;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class GasMiningModule: IUnitModule {
    private static readonly ulong DeathDelay = Convert.ToUInt64(1.415 * Controller.FramesPerSecond) + 5; // +5 just to be sure

    private readonly Unit _worker;
    private readonly Unit _targetGasExtractor;

    public GasMiningModule(Unit worker, Unit targetGasExtractor) {
        _worker = worker;
        _targetGasExtractor = targetGasExtractor;

        // Workers disappear when going inside extractors for 1.415 seconds
        // Change their death delay so that we don't think they're dead
        _worker.DeathDelay = DeathDelay;
    }

    public void Execute() {
        if (_worker.Orders.Count == 0 || _worker.Orders.Count == 1 && _worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != _targetGasExtractor.Tag)) {
            _worker.Gather(_targetGasExtractor);
        }

        Debugger.AddLine(_worker.Position, _targetGasExtractor.Position, Colors.Lime);
    }
}
