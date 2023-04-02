namespace Bot.UnitModules;

public class MiningModule: UnitModule {
    public const string Tag = "MiningModule";

    private readonly Unit _worker;
    private IStrategy _strategy;

    public Resources.ResourceType ResourceType;
    public Unit AssignedResource;

    public static void Install(Unit worker, Unit assignedResource) {
        if (PreInstallCheck(Tag, worker)) {
            worker.Modules.Add(Tag, new MiningModule(worker, assignedResource));
        }
    }

    private MiningModule(Unit worker, Unit assignedResource) {
        _worker = worker;

        if (assignedResource != null) {
            AssignResource(assignedResource, releasePreviouslyAssignedResource: false);
        }
        else {
            Disable();
        }
    }

    protected override void DoExecute() {
        _strategy.Execute();
    }

    /// <summary>
    /// Assign a new resource to this mining module.
    /// If releaseAssignedResource is true and there is an assigned resource, it will be released and its capacity module will be updated.
    /// If the newlyAssignedResource is non null, it will be assigned and its capacity module will be updated.
    /// </summary>
    /// <param name="newlyAssignedResource"></param>
    /// <param name="releasePreviouslyAssignedResource"></param>
    public void AssignResource(Unit newlyAssignedResource, bool releasePreviouslyAssignedResource) {
        if (!releasePreviouslyAssignedResource && AssignedResource != null) {
            Logger.Error("MiningModule trying to assign a resource but one is already assigned");
            return;
        }

        if (releasePreviouslyAssignedResource) {
            ReleaseResource(updateCapacityModule: true);
        }

        if (newlyAssignedResource == null) {
            return;
        }

        Get<CapacityModule>(newlyAssignedResource).Assign(_worker);

        ResourceType = Resources.GetResourceType(newlyAssignedResource);
        AssignedResource = newlyAssignedResource;

        _strategy = ResourceType switch
        {
            Resources.ResourceType.Gas => new GasMiningStrategy(_worker, newlyAssignedResource),
            Resources.ResourceType.Mineral => new MineralMiningStrategy(_worker, newlyAssignedResource),
            _ => null
        };

        Enable();
    }

    /// <summary>
    /// Release the currently assigned resource of this mining module.
    /// If it is non null and updateCapacityModule is true, its capacity module will also be updated.
    /// </summary>
    public void ReleaseResource(bool updateCapacityModule) {
        if (updateCapacityModule && AssignedResource != null) {
            Get<CapacityModule>(AssignedResource).Release(_worker);
        }

        ResourceType = Resources.ResourceType.None;
        AssignedResource = null;
        _strategy = null;

        Disable();
    }

    protected override void OnUninstall() {
        if (AssignedResource == null) {
            return;
        }

        var resourceCapacityModule = Get<CapacityModule>(AssignedResource);
        if (resourceCapacityModule != null) {
            resourceCapacityModule.Release(_worker);
        }
        else {
            Logger.Error("({0}) Assigned resource {1} had no capacity module during uninstallation", this, AssignedResource);
        }
    }

    public override string ToString() {
        return $"{_worker}_{Tag}";
    }
}
