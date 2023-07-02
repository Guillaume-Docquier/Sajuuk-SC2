using System;
using System.Linq;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.Utils;

namespace Sajuuk.UnitModules;

public class GasMiningStrategy: IStrategy {
    private static readonly ulong GasDeathDelay = Convert.ToUInt64(1.415 * TimeUtils.FramesPerSecond) + 5; // +5 just to be sure

    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly Unit _worker;
    private readonly Unit _resource;

    public GasMiningStrategy(IGraphicalDebugger graphicalDebugger, Unit worker, Unit resource) {
        // Workers disappear when going inside extractors for 1.415 seconds
        // Change their death delay so that we don't think they're dead
        worker.DeathDelay = GasDeathDelay;

        _graphicalDebugger = graphicalDebugger;
        _worker = worker;
        _resource = resource;
    }

    public void Execute() {
        if (!_worker.Orders.Any() || _worker.Orders.Any(order => Abilities.Gather.Contains(order.AbilityId) && order.TargetUnitTag != _resource.Tag)) {
            _worker.Gather(_resource);
        }

        _graphicalDebugger.AddLine(_worker.Position, _resource.Position, Colors.LimeGreen);
    }
}
