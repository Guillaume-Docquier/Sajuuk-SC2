using System.Linq;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.Managers.EconomyManagement.TownHallSupervision;

namespace Bot.UnitModules;

/// <summary>
/// A strategy to mine minerals.
/// It implements speed mining on patches with 2 or less workers.
///
/// Speed mining benchmarking method:
/// - We train 1 overlord @13 and train drones until we have 16. (BuildOrders.TestSpeedMining)
/// - We disable manager requests
/// - We let the game run until frame 2016 (1:30 game time)
/// - We read Controller.Observation.Observation.Score.ScoreDetails.CollectedMinerals
///
/// Benchmark on Stargazers top left
/// - Normal mining: 1240 minerals
/// - Speed mining: 1305 minerals (+5.24%)
/// </summary>
public class MineralMiningStrategy: IStrategy {
    private const float SpeedMiningDistanceThreshold = 0.4f; // Empirically tested, do not go lower

    private readonly Unit _worker;
    private readonly Unit _mineral;

    public MineralMiningStrategy(Unit worker, Unit mineral) {
        _worker = worker;
        _mineral = mineral;
    }

    public void Execute() {
        if (CanDoSpeedMining() && ShouldDoSpeedMining()) {
            ReturnCargo();
        }
        else {
            Gather();
        }

        Program.GraphicalDebugger.AddLine(_worker.Position, _mineral.Position, Colors.Cyan);
    }

    private bool CanDoSpeedMining() {
        return UnitModule.Get<CapacityModule>(_mineral).AssignedUnits.Count <= 2;
    }

    // TODO GD Handle speed mining on the way back?
    private bool ShouldDoSpeedMining() {
        return IsWorkerCarryingMinerals() && IsCloseEnoughToTownHall();
    }

    private bool IsWorkerCarryingMinerals() {
        return _worker.Buffs.Any(Buffs.CarryMinerals.Contains);
    }

    /// <summary>
    /// We only start speed mining when close to the townHall to benefit from the mineral walk as much as possible.
    /// Also, since we spam orders (see note below), checking this will reduce the amount of orders that we send, which is good for performance.
    /// </summary>
    /// <returns>True if the worker has SpeedMiningDistanceThreshold % or less of the distance to go.</returns>
    private bool IsCloseEnoughToTownHall() {
        var townHall = (_worker.Supervisor as TownHallSupervisor)!.TownHall; // This is not cute nor clean, but it is efficient and we like that

        var distanceBetweenMineralAndTownHall = _mineral.DistanceTo(townHall) - townHall.Radius;
        var distanceToTownHall = _worker.DistanceTo(townHall) - townHall.Radius;

        var percentageOfDistanceRemaining = distanceToTownHall / distanceBetweenMineralAndTownHall;

        return percentageOfDistanceRemaining <= SpeedMiningDistanceThreshold;
    }

    // TODO GD Speed mining only has to do with preventing drone deceleration. You can speed mine on the way back too
    private void ReturnCargo() {
        var townHall = (_worker.Supervisor as TownHallSupervisor)!.TownHall; // This is not cute nor clean, but it is efficient and we like that
        var distanceToTownHall = townHall.DistanceTo(_worker);

        if (distanceToTownHall <= townHall.Radius + _worker.Radius + 0.01f) {
            _worker.ReturnCargo();
        }
        else {
            var targetPosition = townHall.Position.ToVector2().TranslateTowards(_worker.Position.ToVector2(), townHall.Radius);

            // Precision 0f is important.
            // It drastically reduces the spamming of orders.
            // If we set the precision any higher, however, we will negate any speed mining bonuses.
            // I don't really know why, we used to just spam instead.
            _worker.Move(targetPosition, precision: 0f);
        }
    }

    private void Gather() {
        if (!_worker.Orders.Any() || _worker.Orders.Any(order => Abilities.Gather.Contains(order.AbilityId) && order.TargetUnitTag != _mineral.Tag)) {
            _worker.Gather(_mineral);
        }
    }
}
