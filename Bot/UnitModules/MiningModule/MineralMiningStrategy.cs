using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.Managers;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class MineralMiningStrategy: IStrategy {
    private readonly Unit _worker;
    private readonly Unit _resource;

    public MineralMiningStrategy(Unit worker, Unit resource) {
        _worker = worker;
        _resource = resource;
    }

    public void Execute() {
        if (ShouldDoSpeedMining() && IsWorkerCarryingMinerals()) {
            ReturnCargo();
        }
        else {
            Gather();
        }

        GraphicalDebugger.AddLine(_worker.Position, _resource.Position, Colors.Cyan);
    }

    private bool ShouldDoSpeedMining() {
        return UnitModule.Get<CapacityModule>(_resource).AssignedUnits.Count <= 2;
    }

    private bool IsWorkerCarryingMinerals() {
        return _worker.Buffs.Any(Buffs.CarryMinerals.Contains);
    }

    // TODO GD Speed mining only has to do with preventing drone deceleration. You can speed mine on the way back too
    private void ReturnCargo() {
        var townHall = (_worker.Supervisor as TownHallSupervisor)!.TownHall; // This is not cute nor clean, but it is efficient and we like that
        var distanceToTownHall = townHall.HorizontalDistanceTo(_worker);

        if (distanceToTownHall <= townHall.Radius + _worker.Radius + 0.01f) {
            _worker.ReturnCargo();
        }
        else {
            var targetPosition = townHall.Position.TranslateTowards(_worker.Position, townHall.Radius);

            _worker.Move(targetPosition, spam: true);
        }
    }

    private void Gather() {
        if (!_worker.Orders.Any() || _worker.Orders.Any(order => Abilities.Gather.Contains(order.AbilityId) && order.TargetUnitTag != _resource.Tag)) {
            _worker.Gather(_resource);
        }
    }
}
