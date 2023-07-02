using System.Collections.Generic;
using System.Linq;
using Sajuuk.Builds;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.UnitModules;

namespace Sajuuk.Managers;

public class CreepManager : UnitlessManager {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IUnitModuleInstaller _unitModuleInstaller;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public CreepManager(
        IUnitsTracker unitsTracker,
        IUnitModuleInstaller unitModuleInstaller
    ) {
        _unitsTracker = unitsTracker;
        _unitModuleInstaller = unitModuleInstaller;
    }

    protected override void ManagementPhase() {
        foreach (var creepTumor in _unitsTracker.GetUnits(_unitsTracker.NewOwnedUnits, Units.CreepTumor)) {
            _unitModuleInstaller.InstallTumorCreepSpreadModule(creepTumor);
        }
    }

    public override string ToString() {
        return "CreepManager";
    }
}
