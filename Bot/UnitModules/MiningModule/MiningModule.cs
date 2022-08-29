﻿namespace Bot.UnitModules;

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

        ResourceType = Resources.GetResourceType(assignedResource);
        AssignedResource = assignedResource;

        _strategy = ResourceType switch
        {
            Resources.ResourceType.Gas => new GasMiningStrategy(_worker, assignedResource),
            Resources.ResourceType.Mineral => new MineralMiningStrategy(_worker, assignedResource),
            _ => null
        };

        Enable();
    }

    public void ReleaseResource() {
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
