using SC2APIProtocol;

namespace Bot.Tests;

public static class ResponseGameObservationUtils {
    public static ResponseObservation CreateResponseObservation(IEnumerable<Unit> units, uint frame = 0) {
        return CreateResponseObservation(units.Select(unit => unit.RawUnitData), frame);
    }

    public static ResponseObservation CreateResponseObservation(IEnumerable<SC2APIProtocol.Unit> units, uint frame = 0, int maxX = 100, int maxY = 100) {
        // Visibility.Visible == 2
        var visibility = Enumerable.Repeat((byte)2, maxX * maxY).ToList();

        var responseObservation = new ResponseObservation
        {
            Observation = new Observation
            {
                GameLoop = frame,
                PlayerCommon = new PlayerCommon
                {
                    Minerals = 0,
                    Vespene = 0,
                    FoodUsed = 0,
                    FoodCap = 200,
                    PlayerId = 1,
                },
                RawData = new ObservationRaw
                {
                    MapState = new MapState
                    {
                        Visibility = new ImageData
                        {
                            Data = ImageDataUtils.CreateByeString(visibility),
                        }
                    },
                    Player = new PlayerRaw(),
                },
                Score = new Score
                {
                    ScoreDetails = new ScoreDetails
                    {
                        CollectionRateMinerals = 0,
                        CollectionRateVespene = 0,
                    }
                }
            }
        };

        responseObservation.Observation.RawData.Units.AddRange(units);

        return responseObservation;
    }
}
