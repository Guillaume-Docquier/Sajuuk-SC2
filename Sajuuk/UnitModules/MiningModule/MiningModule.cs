using System;
using Sajuuk.Debugging.GraphicalDebugging;

namespace Sajuuk.UnitModules;

public class MiningModule: UnitModule {
    public const string ModuleTag = "MiningModule";

    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly Unit _worker;
    private IStrategy _strategy;

    public Resources.ResourceType ResourceType;
    public Unit AssignedResource;

    public MiningModule(
        IGraphicalDebugger graphicalDebugger,
        Unit worker,
        Unit assignedResource
    ) : base(ModuleTag) {
        _graphicalDebugger = graphicalDebugger;
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
            Resources.ResourceType.Gas => new GasMiningStrategy(_graphicalDebugger, _worker, newlyAssignedResource),
            Resources.ResourceType.Mineral => new MineralMiningStrategy(_graphicalDebugger, _worker, newlyAssignedResource),
            _ => throw new ArgumentException("Cannot create strategy because the assigned resource doesn't have a resource type."),
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
            Logger.Error($"Assigned resource {AssignedResource} had no capacity module during uninstallation");
        }
    }

    public override string ToString() {
        return $"{_worker}_{Tag}";
    }
}
