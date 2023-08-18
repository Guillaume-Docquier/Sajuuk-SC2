using System.Linq;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;

namespace Sajuuk.UnitModules;

public class GasMiningStrategy : IStrategy {
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly Unit _worker;
    private readonly Unit _resource;

    public GasMiningStrategy(IGraphicalDebugger graphicalDebugger, Unit worker, Unit resource) {
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
