using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers;

public class BattleManager: IManager {
    public const float AcceptableDistanceToTarget = 3;

    public Vector3 Target;
    public readonly List<Unit> Army = new List<Unit>();

    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep>();
    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests;

    public BattleManager(Vector3 target) {
        Target = target;
    }

    public void Assign(Vector3 target) {
        Target = target;
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
        Army.Where(unit => !unit.Orders.Any())
            .Where(unit => !unit.RawUnitData.IsBurrowed)
            .Where(unit => unit.DistanceTo(Target) > AcceptableDistanceToTarget)
            .ToList()
            .ForEach(unit => unit.AttackMove(Target));
    }

    public void Retire() {
        Army.ForEach(soldier => BurrowMicroModule.Uninstall(soldier));
    }

    public void ReportUnitDeath(Unit deadUnit) {
        Army.Remove(deadUnit);
    }
}
