namespace Bot.UnitModules;

public class MiningModule: UnitModule {
    public const string Tag = "MiningModule";

    private readonly Unit _worker;
    private IStrategy _strategy;

    public UnitUtils.ResourceType ResourceType;
    public Unit AssignedResource;

    public static void Install(Unit worker, Unit assignedResource) {
        if (PreInstallCheck(Tag, worker)) {
            worker.Modules.Add(Tag, new MiningModule(worker, assignedResource));
        }
    }

    private MiningModule(Unit worker, Unit assignedResource) {
        _worker = worker;

        if (assignedResource != null) {
            AssignResource(assignedResource);
        }
        else {
            Disable();
        }
    }

    protected override void DoExecute() {
        _strategy.Execute();
    }

    public void AssignResource(Unit assignedResource) {
        if (AssignedResource != null) {
            Logger.Error("MiningModule trying to assign a resource but one is already assigned");
            return;
        }

        ResourceType = UnitUtils.GetResourceType(assignedResource);
        AssignedResource = assignedResource;

        _strategy = ResourceType switch
        {
            UnitUtils.ResourceType.Gas => new GasMiningStrategy(_worker, assignedResource),
            UnitUtils.ResourceType.Mineral => new MineralMiningStrategy(_worker, assignedResource),
            _ => null
        };

        Enable();
    }

    public void ReleaseResource() {
        ResourceType = UnitUtils.ResourceType.None;
        AssignedResource = null;
        _strategy = null;

        Disable();
    }
}
