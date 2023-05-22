using Bot.GameData;
using Bot.Tests.Fixtures;

namespace Bot.Tests;

public class BaseTestClass :
    IClassFixture<NoLoggerFixture>,
    IDisposable {
    protected readonly KnowledgeBase KnowledgeBase;

    // TODO GD Remove/replace the commented code once the DI refactor is complete
    protected BaseTestClass() {
        KnowledgeBase = new KnowledgeBase();
        KnowledgeBase.Data = KnowledgeBaseDataStore.Load();
    }

    public void Dispose() {
        //ControllerTearDown();
    }

    private static void ControllerSetup() {
        //Controller.Reset();

        //var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo();

        //var units = GetInitialUnits();
        //var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        //Controller.NewFrame(gameInfo, observation);
    }

    //private static void ControllerTearDown() {
    //    Controller.Reset();
    //}

    //public static List<Unit> GetInitialUnits() {
    //    return new List<Unit>
    //        {
    //            TestUtils.CreateUnit(Units.Hatchery, position: new Vector3(10.5f , 10.5f, 0)),
    //        }
    //        // Own
    //        .Concat(GenerateExpandResources(10.5f , 10.5f)) // Main
    //        .Concat(GenerateExpandResources(30.5f , 10.5f)) // Nat
    //        .Concat(GenerateExpandResources(50.5f , 10.5f)) // Third
    //        .Concat(GenerateExpandResources(70.5f , 10.5f)) // Fourth
    //        .Concat(GenerateExpandResources(90.5f , 10.5f)) // Fifth
    //        // Enemy
    //        .Concat(GenerateExpandResources(90.5f, 80.5f)) // Main
    //        .Concat(GenerateExpandResources(70.5f, 80.5f)) // Nat
    //        .Concat(GenerateExpandResources(50.5f, 80.5f)) // Third
    //        .Concat(GenerateExpandResources(30.5f, 80.5f)) // Fourth
    //        .Concat(GenerateExpandResources(10.5f, 80.5f)) // Fifth
    //        .ToList();
    //}

    //private static List<Unit> GenerateExpandResources(float expandX, float expandY) {
    //    return new List<Unit>
    //    {
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY - 5, 0)),
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY - 4, 0)),
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY - 3, 0)),
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY - 2, 0)),
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY - 1, 0)),
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY - 0, 0)),
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY + 1, 0)),
    //        TestUtils.CreateUnit(Units.MineralField750, position: new Vector3(expandX - 3.5f, expandY + 2, 0)),
    //        TestUtils.CreateUnit(Units.VespeneGeyser,   position: new Vector3(expandX - 1f  , expandY - 4, 0)),
    //        TestUtils.CreateUnit(Units.VespeneGeyser,   position: new Vector3(expandX + 2f  , expandY - 4, 0)),
    //    };
    //}
}
