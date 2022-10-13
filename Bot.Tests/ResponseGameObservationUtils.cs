using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Google.Protobuf;
using SC2APIProtocol;

namespace Bot.Tests;

public static class ResponseGameObservationUtils {
    public static ResponseObservation CreateResponseObservation(IEnumerable<Unit>? units = null, uint frame = 0) {
        var visibility = Enumerable.Repeat(true, MapAnalyzer.MaxX * MapAnalyzer.MaxY).ToList();

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
            }
        };

        if (units != null) {
            responseObservation.Observation.RawData.Units.AddRange(units.Select(ToSc2ApiProtocolUnit));
        }

        return responseObservation;
    }

    private static SC2APIProtocol.Unit ToSc2ApiProtocolUnit(Unit unit) {
        return new SC2APIProtocol.Unit
        {
            Tag = unit.Tag,
            UnitType = unit.UnitType,
            Alliance = unit.Alliance,
            Pos = unit.Position.ToPoint(),
        };
    }
}
