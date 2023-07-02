using SC2APIProtocol;

namespace Sajuuk.UnitModules;

public interface IUnitModuleInstaller {
    public MiningModule InstallMiningModule(Unit worker, Unit assignedResource);
    public AttackPriorityModule InstallAttackPriorityModule(Unit unit);
    public CapacityModule InstallCapacityModule(Unit unit, int maxCapacity, bool showDebugInfo = true);
    public ChangelingTargetingModule InstallChangelingTargetingModule(Unit unit);
    public DebugLocationModule InstallDebugLocationModule(Unit unit, Color color = null, bool showName = false);
    public QueenMicroModule InstallQueenMicroModule(Unit queen, Unit assignedTownHall);
    public TumorCreepSpreadModule InstallTumorCreepSpreadModule(Unit creepTumor);
}
