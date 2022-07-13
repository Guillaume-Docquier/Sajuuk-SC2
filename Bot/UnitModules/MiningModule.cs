using System;
using System.Linq;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class MiningModule: IUnitModule {
    public const string Tag = "mining-module";

    private static readonly ulong GasDeathDelay = Convert.ToUInt64(1.415 * Controller.FramesPerSecond) + 5; // +5 just to be sure

    private delegate void Mining();

    private readonly Mining _work;
    private readonly Unit _worker;
    private readonly Unit _assignedResource;

    public static void Install(Unit worker, Unit assignedResource) {
        worker.Modules.Remove(Tag);
        worker.Modules.Add(Tag, new MiningModule(worker, assignedResource));
    }

    private MiningModule(Unit worker, Unit assignedResource) {
        _worker = worker;
        _assignedResource = assignedResource;

        var resourceType = UnitUtils.GetResourceType(assignedResource);

        // Workers disappear when going inside extractors for 1.415 seconds
        // Change their death delay so that we don't think they're dead
        if (resourceType == UnitUtils.ResourceType.Gas) {
            _worker.DeathDelay = GasDeathDelay;
            _work = GasMining;
        }
        else if (resourceType == UnitUtils.ResourceType.Mineral) {
            _work = MineralMining;
        }
    }

    public void Execute() {
        _work();
    }

    private void MineralMining() {
        // Make sure each worker gathers from its target patch
        // TODO GD Fast mining consists of moving to the base, not using return cargo
        if (_worker.Orders.Count == 0 || _worker.Orders.Count == 1 && _worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != _assignedResource.Tag)) {
            _worker.Gather(_assignedResource);
        }

        Debugger.AddLine(_worker.Position, _assignedResource.Position, Colors.Cyan);
    }

    private void GasMining() {
        if (_worker.Orders.Count == 0 || _worker.Orders.Count == 1 && _worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != _assignedResource.Tag)) {
            _worker.Gather(_assignedResource);
        }

        Debugger.AddLine(_worker.Position, _assignedResource.Position, Colors.Lime);
    }
}
