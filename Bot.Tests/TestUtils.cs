using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Tests;

public static class TestUtils {
    private static ulong _currentTag = 0;

    public static SC2APIProtocol.Unit CreateUnitRaw(
        uint unitType,
        Alliance alliance = Alliance.Self,
        Vector3 position = default,
        int vespeneContents = 0,
        float buildProgress = 1f
    ) {
        if (Units.MineralFields.Contains(unitType) || Units.GasGeysers.Contains(unitType)) {
            alliance = Alliance.Neutral;
        }

        var rawUnit = new SC2APIProtocol.Unit
        {
            Tag = Interlocked.Increment(ref _currentTag),
            UnitType = unitType,
            Alliance = alliance,
            Pos = position.ToPoint(),
            VespeneContents = vespeneContents,
            BuildProgress = buildProgress,
        };

        return rawUnit;
    }

    // TODO GD Create a factory instead of this helper function
    public static Unit CreateUnit(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IActionBuilder actionBuilder,
        IActionService actionService,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IUnitsTracker unitsTracker,
        uint unitType,
        uint frame = 0,
        Alliance alliance = Alliance.Self,
        Vector3 position = default,
        int vespeneContents = 0,
        float buildProgress = 1f) {
        var rawUnit = CreateUnitRaw(unitType, alliance, position, vespeneContents, buildProgress);

        // TODO GD I really need an IUnit interface
        return new Unit(frameClock, knowledgeBase, actionBuilder, actionService, terrainTracker, regionsTracker, unitsTracker, rawUnit, frame);
    }

    public class DummyManager: Manager {
        public override IEnumerable<BuildFulfillment> BuildFulfillments { get; } = Enumerable.Empty<BuildFulfillment>();

        protected override IAssigner Assigner { get; }
        protected override IDispatcher Dispatcher { get; }
        protected override IReleaser Releaser { get; }

        public DummyManager(IAssigner? assigner = null, IDispatcher? dispatcher = null, IReleaser? releaser = null) {
            Assigner = assigner ?? new DummyAssigner();
            Dispatcher = dispatcher ?? new DummyDispatcher();
            Releaser = releaser ?? new DummyReleaser();
        }

        protected override void RecruitmentPhase() {}

        protected override void DispatchPhase() {}

        protected override void ManagementPhase() {}
    }

    public class DummySupervisor : Supervisor {
        public override IEnumerable<BuildFulfillment> BuildFulfillments { get; } = Enumerable.Empty<BuildFulfillment>();

        protected override IAssigner Assigner { get; }
        protected override IReleaser Releaser { get; }

        public DummySupervisor(IAssigner? assigner = null, IReleaser? releaser = null) {
            Assigner = assigner ?? new DummyAssigner();
            Releaser = releaser ?? new DummyReleaser();
        }

        protected override void Supervise() {}

        public override void Retire() {}
    }

    private class DummyAssigner: IAssigner {
        public void Assign(Unit unit) {}
    }

    private class DummyDispatcher: IDispatcher {
        public void Dispatch(Unit unit) {}
    }

    private class DummyReleaser: IReleaser {
        public void Release(Unit unit) {}
    }
}
