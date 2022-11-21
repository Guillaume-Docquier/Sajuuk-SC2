using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.Managers;
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
            Tag = _currentTag,
            UnitType = unitType,
            Alliance = alliance,
            Pos = position.ToPoint(),
            VespeneContents = vespeneContents,
            BuildProgress = buildProgress,
        };

        // Just make sure to never collide
        _currentTag++;

        return rawUnit;
    }

    public static Unit CreateUnit(
        uint unitType,
        uint frame = 0,
        Alliance alliance = Alliance.Self,
        Vector3 position = default,
        int vespeneContents = 0,
        float buildProgress = 1f
    ) {
        var rawUnit = CreateUnitRaw(unitType, alliance, position, vespeneContents, buildProgress);

        return new Unit(rawUnit, frame);
    }

    public static void NewFrame(ResponseObservation observation) {
        Controller.NewFrame(ResponseGameInfoUtils.CreateResponseGameInfo(), observation);
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
