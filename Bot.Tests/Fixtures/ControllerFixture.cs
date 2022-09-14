using System.Numerics;
using Bot.GameData;

namespace Bot.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class ControllerFixture {
    public ControllerFixture() {
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo();
        Controller.NewGameInfo(gameInfo);

        var startingTownHall = TestUtils.CreateUnit(Units.Hatchery, position: new Vector3(0, 0, 0));
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: new List<Unit> { startingTownHall });
        Controller.NewObservation(observation);
    }
}
