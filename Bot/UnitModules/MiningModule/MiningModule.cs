using Bot.Wrapper;

namespace Bot.UnitModules;

public class MiningModule: IUnitModule {
    public const string Tag = "MiningModule";

    private readonly IStrategy _strategy;

    public readonly UnitUtils.ResourceType ResourceType;
    public readonly Unit AssignedResource;

    public static void Install(Unit worker, Unit assignedResource) {
        if (UnitModule.PreInstallCheck(Tag, worker)) {
            worker.Modules.Add(Tag, new MiningModule(worker, assignedResource));
        }
    }

    private MiningModule(Unit worker, Unit assignedResource) {
        ResourceType = UnitUtils.GetResourceType(assignedResource);
        AssignedResource = assignedResource;

        _strategy = ResourceType switch
        {
            UnitUtils.ResourceType.Gas => new GasMiningStrategy(worker, assignedResource),
            UnitUtils.ResourceType.Mineral => new MineralMiningStrategy(worker, assignedResource),
            _ => null
        };
    }

    public void Execute() {
        _strategy.Execute();
    }
}
