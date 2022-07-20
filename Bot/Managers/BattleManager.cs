using System;
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

    public float Force => GetForceOf(Army);

    private float RetreatForceThreshold => _initialForce * 0.5f;
    private float _initialForce;

    private float AttackForceThreshold => _strongestForce * 1.2f;
    private float _strongestForce;

    private bool _attackStarted = false;

    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep>();
    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests;

    public BattleManager(Vector3 target) {
        Target = target;
    }

    public void Assign(Vector3 target) {
        Target = target.WithWorldHeight();
        _initialForce = Force;
        _strongestForce = Force;
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
        if (biggestCluster == null || ShouldRetreat(biggestCluster)) {
            _attackStarted = false;

            Retreat(Clustering.GetCenter(Army), Army);
        }
        else if (ShouldGrowStronger(biggestCluster)) {
            _initialForce = GetForceOf(biggestCluster);

            var clusterCenter = Clustering.GetCenter(biggestCluster).Translate(1f, 1f);
            GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {GetForceOf(biggestCluster)}",
                    $"Strongest: {_strongestForce}",
                    $"Attack at: {AttackForceThreshold}"
                },
                worldPos: clusterCenter.ToPoint());

            Retreat(Clustering.GetCenter(Army), Army);
        }
        else {
            _attackStarted = true;
            _strongestForce = Math.Max(_strongestForce, GetForceOf(biggestCluster));

            var clusterCenter = Clustering.GetCenter(biggestCluster).Translate(1f, 1f);
            GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {GetForceOf(biggestCluster)}",
                    $"Initial: {_initialForce}",
                    $"Retreat at: {RetreatForceThreshold}"
                },
                worldPos: clusterCenter.ToPoint());

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

    private void Retreat(Vector3 retreat, IReadOnlyCollection<Unit> soldiers) {
        if (soldiers.Count <= 0) {
            return;
        }

        GraphicalDebugger.AddSphere(retreat, AcceptableDistanceToTarget, Colors.Yellow);
        GraphicalDebugger.AddText("Retreat", worldPos: retreat.ToPoint());

        soldiers.Where(unit => unit.Orders.All(order => order.TargetWorldSpacePos == null || !order.TargetWorldSpacePos.Equals(retreat.ToPoint())))
            .Where(unit => unit.DistanceTo(retreat) > AcceptableDistanceToTarget)
            .ToList()
            .ForEach(unit => unit.Move(retreat));

        foreach (var soldier in soldiers) {
            GraphicalDebugger.AddLine(soldier.Position, retreat, Colors.Yellow);
        }
    }

    private static void Attack(Vector3 target, IReadOnlyCollection<Unit> soldiers) {
        if (soldiers.Count <= 0) {
            return;
        }

        GraphicalDebugger.AddSphere(target, AcceptableDistanceToTarget, Colors.Red);
        GraphicalDebugger.AddText("Attack", worldPos: target.ToPoint());

        soldiers.Where(unit => unit.Orders.All(order => order.AbilityId is Abilities.Move or Abilities.Attack))
            .Where(unit => !unit.RawUnitData.IsBurrowed)
            .Where(unit => unit.DistanceTo(target) > AcceptableDistanceToTarget)
            .ToList()
            .ForEach(unit => unit.AttackMove(target));

        foreach (var soldier in soldiers) {
            GraphicalDebugger.AddLine(soldier.Position, target, Colors.Red);
        }
    }

    private static void Rally(Vector3 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
        if (soldiers.Count <= 0) {
            return;
        }

        GraphicalDebugger.AddSphere(rallyPoint, AcceptableDistanceToTarget, Colors.DarkGreen);
        GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToPoint());

        soldiers.Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget)
            .ToList()
            .ForEach(unit => unit.AttackMove(rallyPoint));

        foreach (var soldier in soldiers) {
            GraphicalDebugger.AddLine(soldier.Position, rallyPoint, Colors.DarkGreen);
        }
    }

    private static float GetForceOf(IEnumerable<Unit> soldiers) {
        return soldiers.Sum(soldier => soldier.FoodRequired);
    }

    private bool ShouldRetreat(IEnumerable<Unit> soldiers) {
        return _attackStarted && GetForceOf(soldiers) < RetreatForceThreshold;
    }

    private bool ShouldGrowStronger(IEnumerable<Unit> soldiers) {
        return !_attackStarted && GetForceOf(soldiers) < AttackForceThreshold && !Controller.IsSupplyCapped; // TODO GD Not exactly
    }
}
