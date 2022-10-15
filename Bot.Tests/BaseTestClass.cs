using System.Numerics;
using Bot.GameData;
using Bot.Tests.Fixtures;

namespace Bot.Tests;

// Collection("Sequential") makes it so each test class is part of the same collection
// This will make the tests run sequentially
// It is required because all state is global (oops) and setup/teardown might affect other tests
// This is notably caused by the Controller and its friends WhoNeedUpdating
[Collection("Sequential")]
public class BaseTestClass :
    IClassFixture<LoggerFixture>,
    IClassFixture<KnowledgeBaseFixture>,
    IClassFixture<GraphicalDebuggerFixture>,
    IDisposable {
    protected BaseTestClass() {
        ControllerSetup();
    }

    public void Dispose() {
        ControllerTearDown();
    }

    private static void ControllerSetup() {
        Controller.Reset();

        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo();
        Controller.NewGameInfo(gameInfo);

        var startingTownHall = TestUtils.CreateUnit(Units.Hatchery, position: new Vector3(0, 0, 0));
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: new List<Unit> { startingTownHall });
        Controller.NewObservation(observation);
    }

    private static void ControllerTearDown() {
        Controller.Reset();
    }
}
