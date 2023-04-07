using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision;

public class RegionalArmySupervisor : Supervisor {
    private readonly IUnitsControl _unitsController = new UnitsController();
    private readonly Region _assignedRegion;

    // TODO GD Rework assigner/releaser. It's not helpful at all
    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IReleaser Releaser { get; } = new DummyReleaser();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public RegionalArmySupervisor(Region assignedRegion) {
        _assignedRegion = assignedRegion;
    }

    protected override void Supervise() {
        // TODO GD Implement this for real
        // Make your way to the target region
        // Group up
        // Attack with units that are ready, rally the others

        var unhandledUnits = _unitsController.Execute(SupervisedUnits);
        foreach (var unhandledUnit in unhandledUnits) {
            unhandledUnit.AttackMove(_assignedRegion.Center);
        }
    }

    public override void Retire() {
        foreach (var supervisedUnit in SupervisedUnits) {
            Release(supervisedUnit);
        }
    }

    public IEnumerable<Unit> GetReleasableUnits() {
        // TODO GD Implement this for real
        // Units not necessary to a current fight can be released
        return Enumerable.Empty<Unit>();
    }

    private class DummyAssigner : IAssigner { public void Assign(Unit unit) {} }
    private class DummyReleaser : IReleaser { public void Release(Unit unit) {} }
}
