using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.UnitModules;
using Bot.Wrapper;

namespace Bot.Managers;

public class BattleManager: IManager {
    public const float AcceptableDistanceToTarget = 3;

    public Vector3 Target;
    public readonly List<Unit> Army = new List<Unit>();
    public float Force => Army.Sum(soldier => soldier.FoodRequired);
    public float MinimumForce => Force * 0.5f;

    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep>();
    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests;

    public BattleManager(Vector3 target) {
        Target = target;
    }

    public void Assign(Vector3 target) {
        Target = Pathfinder.WithWorldHeight(target);
    }

    public void Assign(List<Unit> soldiers) {
        soldiers.ForEach(unit => {
            unit.AddDeathWatcher(this);

            if (unit.UnitType == Units.Roach) {
                BurrowMicroModule.Install(unit);
            }
        });

        // TODO GD Use a targeting module
        Army.AddRange(soldiers);
    }

    public void OnFrame() {
        if (Army.Count < 1) {
            return;
        }

        var clusters = Clustering.DBSCAN(Army, 3, 3).OrderByDescending(cluster => cluster.Count).ToList();

        var biggestCluster = clusters.FirstOrDefault();
        if (biggestCluster == null || biggestCluster.Sum(soldier => soldier.FoodRequired) < MinimumForce) {
            Retreat(Clustering.GetCenter(Army), Army);
        }
        else {
            Attack(Target, biggestCluster);
            Rally(Clustering.GetCenter(biggestCluster), Army.Where(soldier => !biggestCluster.Contains(soldier)).ToList());
        }
    }

    public void Retire() {
        Army.ForEach(soldier => UnitModule.Uninstall<BurrowMicroModule>(soldier));
    }

    public void ReportUnitDeath(Unit deadUnit) {
        Army.Remove(deadUnit);
    }

    private static void Retreat(Vector3 retreat, IReadOnlyCollection<Unit> soldiers) {
        if (soldiers.Count <= 0) {
            return;
        }

        GraphicalDebugger.AddSphere(retreat, AcceptableDistanceToTarget, Colors.Yellow);
        GraphicalDebugger.AddText("Retreat", worldPos: retreat.ToPoint());

        soldiers.Where(unit => unit.Orders.All(order => order.TargetWorldSpacePos == null || !order.TargetWorldSpacePos.Equals(retreat.ToPoint())))
            .Where(unit => !unit.RawUnitData.IsBurrowed)
            .Where(unit => unit.DistanceTo(retreat) > AcceptableDistanceToTarget)
            .ToList()
            .ForEach(unit => {
                unit.Move(retreat);
                GraphicalDebugger.AddLine(unit.Position, retreat, Colors.Yellow);
            });
    }

    private static void Attack(Vector3 target, IReadOnlyCollection<Unit> soldiers) {
        if (soldiers.Count <= 0) {
            return;
        }

        GraphicalDebugger.AddSphere(target, AcceptableDistanceToTarget, Colors.Red);
        GraphicalDebugger.AddText("Attack", worldPos: target.ToPoint());

        soldiers.Where(unit => !unit.Orders.Any())
            .Where(unit => !unit.RawUnitData.IsBurrowed)
            .Where(unit => unit.DistanceTo(target) > AcceptableDistanceToTarget)
            .ToList()
            .ForEach(unit => {
                unit.AttackMove(target);
                GraphicalDebugger.AddLine(unit.Position, target, Colors.Red);
            });
    }

    private static void Rally(Vector3 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
        if (soldiers.Count <= 0) {
            return;
        }

        GraphicalDebugger.AddSphere(rallyPoint, AcceptableDistanceToTarget, Colors.DarkGreen);
        GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToPoint());

        soldiers.Where(unit => !unit.RawUnitData.IsBurrowed)
            .Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget)
            .ToList()
            .ForEach(unit => {
                unit.Move(rallyPoint);
                GraphicalDebugger.AddLine(unit.Position, rallyPoint, Colors.DarkGreen);
            });
    }
}
