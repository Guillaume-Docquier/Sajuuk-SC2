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

    // TODO GD Speed mining only has to do with forcing drone to not decelerate. You can speed maine on the way back too
    // https://tl.net/forum/starcraft-2/152345-trick-early-game-7-mineral-boost
    // If you carefully observe your workers mining, you will notice that they sit around for half a second after they
    // mine a mineral before realizing they need to return it to your CC. I'm not sure why the AI is slow on this, but
    // the delay is actually not necessary. By removing this delay, mining speed is increased by up to 7%.
    private void ReturnCargo() {
        // This is not cute, but it'll work
        var manager = _worker.Supervisor as TownHallManager;
        var townHall = manager!.TownHall;
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
