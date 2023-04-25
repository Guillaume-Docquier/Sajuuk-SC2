using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;

namespace Bot.MapKnowledge;

public class FootprintCalculator {
    private readonly IMapAnalyzer _mapAnalyzer;

    // TODO GD It would be nice if we didn't need the map analyzer
    public FootprintCalculator(IMapAnalyzer mapAnalyzer) {
        _mapAnalyzer = mapAnalyzer;
    }

    public List<Vector2> GetFootprint(Unit obstacle) {
        if (Units.MineralFields.Contains(obstacle.UnitType)) {
            return GetMineralFootprint(obstacle);
        }

        if (Units.Obstacles.Contains(obstacle.UnitType)) {
            return GetRockFootprint(obstacle);
        }

        return GetGenericFootprint(obstacle);
    }

    private static List<Vector2> GetMineralFootprint(Unit mineral) {
        // Mineral fields are 1x2
        return new List<Vector2>
        {
            mineral.Position.Translate(xTranslation: -0.5f).AsWorldGridCenter().ToVector2(),
            mineral.Position.Translate(xTranslation: 0.5f).AsWorldGridCenter().ToVector2(),
        };
    }

    private List<Vector2> GetRockFootprint(Unit rock) {
        var footprint = new List<Vector2>();
        switch (rock.UnitType) {
            case Units.DestructibleDebris4x4:
            case Units.DestructibleRock4x4:
            case Units.DestructibleRockEx14x4:
                footprint.AddRange(new Vector2[]
                {
                    new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f),
                    new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f),
                    new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f),
                    new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f),
                });
                break;
            case Units.DestructibleCityDebris6x6:
            case Units.DestructibleDebris6x6:
            case Units.DestructibleRock6x6:
            case Units.DestructibleRockEx16x6:
                footprint.AddRange(new Vector2[]
                {
                                       new(-1.5f,  2.5f), new(-0.5f,  2.5f), new(0.5f,  2.5f), new(1.5f,  2.5f),
                    new(-2.5f,  1.5f), new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f), new(2.5f,  1.5f),
                    new(-2.5f,  0.5f), new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f), new(2.5f,  0.5f),
                    new(-2.5f, -0.5f), new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f), new(2.5f, -0.5f),
                    new(-2.5f, -1.5f), new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f), new(2.5f, -1.5f),
                                       new(-1.5f, -2.5f), new(-0.5f, -2.5f), new(0.5f, -2.5f), new(1.5f, -2.5f),
                });
                break;
            case Units.DestructibleRampDiagonalHugeBLUR:
            case Units.DestructibleDebrisRampDiagonalHugeBLUR:
            case Units.DestructibleCityDebrisHugeDiagonalBLUR:
                footprint.AddRange(new Vector2[]
                {
                                                                                                                                     new(1.5f,  4.5f), new(2.5f,  4.5f), new(3.5f,  4.5f),
                                                                                                                   new(0.5f,  3.5f), new(1.5f,  3.5f), new(2.5f,  3.5f), new(3.5f,  3.5f), new(4.5f,  3.5f),
                                                                                                new(-0.5f,  2.5f), new(0.5f,  2.5f), new(1.5f,  2.5f), new(2.5f,  2.5f), new(3.5f,  2.5f), new(4.5f,  2.5f),
                                                                             new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f), new(2.5f,  1.5f), new(3.5f,  1.5f), new(4.5f,  1.5f),
                                                          new(-2.5f,  0.5f), new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f), new(2.5f,  0.5f), new(3.5f,  0.5f),
                                       new(-3.5f, -0.5f), new(-2.5f, -0.5f), new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f), new(2.5f, -0.5f),
                    new(-4.5f, -1.5f), new(-3.5f, -1.5f), new(-2.5f, -1.5f), new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f),
                    new(-4.5f, -2.5f), new(-3.5f, -2.5f), new(-2.5f, -2.5f), new(-1.5f, -2.5f), new(-0.5f, -2.5f), new(0.5f, -2.5f),
                    new(-4.5f, -3.5f), new(-3.5f, -3.5f), new(-2.5f, -3.5f), new(-1.5f, -3.5f), new(-0.5f, -3.5f),
                                       new(-3.5f, -4.5f), new(-2.5f, -4.5f), new(-1.5f, -4.5f),
                });
                break;
            case Units.DestructibleRampDiagonalHugeULBR:
            case Units.DestructibleDebrisRampDiagonalHugeULBR:
            case Units.DestructibleRockEx1DiagonalHugeULBR:
                footprint.AddRange(new Vector2[]
                {
                                       new(-3.5f,  4.5f), new(-2.5f,  4.5f), new(-1.5f,  4.5f),
                    new(-4.5f,  3.5f), new(-3.5f,  3.5f), new(-2.5f,  3.5f), new(-1.5f,  3.5f), new(-0.5f,  3.5f),
                    new(-4.5f,  2.5f), new(-3.5f,  2.5f), new(-2.5f,  2.5f), new(-1.5f,  2.5f), new(-0.5f,  2.5f), new(0.5f,  2.5f),
                    new(-4.5f,  1.5f), new(-3.5f,  1.5f), new(-2.5f,  1.5f), new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f),
                                       new(-3.5f,  0.5f), new(-2.5f,  0.5f), new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f), new(2.5f,  0.5f),
                                                          new(-2.5f, -0.5f), new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f), new(2.5f, -0.5f), new(3.5f, -0.5f),
                                                                             new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f), new(2.5f, -1.5f), new(3.5f, -1.5f), new(4.5f, -1.5f),
                                                                                                new(-0.5f, -2.5f), new(0.5f, -2.5f), new(1.5f, -2.5f), new(2.5f, -2.5f), new(3.5f, -2.5f), new(4.5f, -2.5f),
                                                                                                                   new(0.5f, -3.5f), new(1.5f, -3.5f), new(2.5f, -3.5f), new(3.5f, -3.5f), new(4.5f, -3.5f),
                                                                                                                                     new(1.5f, -4.5f), new(2.5f, -4.5f), new(3.5f, -4.5f),
                });
                break;
            case Units.DestructibleRampVerticalHuge:
                footprint.AddRange(new Vector2[]
                {
                    new(-1.5f,  5.5f), new(-0.5f,  5.5f), new(0.5f,  5.5f), new(1.5f,  5.5f),
                    new(-1.5f,  4.5f), new(-0.5f,  4.5f), new(0.5f,  4.5f), new(1.5f,  4.5f),
                    new(-1.5f,  3.5f), new(-0.5f,  3.5f), new(0.5f,  3.5f), new(1.5f,  3.5f),
                    new(-1.5f,  2.5f), new(-0.5f,  2.5f), new(0.5f,  2.5f), new(1.5f,  2.5f),
                    new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f),
                    new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f),
                    new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f),
                    new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f),
                    new(-1.5f, -2.5f), new(-0.5f, -2.5f), new(0.5f, -2.5f), new(1.5f, -2.5f),
                    new(-1.5f, -3.5f), new(-0.5f, -3.5f), new(0.5f, -3.5f), new(1.5f, -3.5f),
                    new(-1.5f, -4.5f), new(-0.5f, -4.5f), new(0.5f, -4.5f), new(1.5f, -4.5f),
                    new(-1.5f, -5.5f), new(-0.5f, -5.5f), new(0.5f, -5.5f), new(1.5f, -5.5f),
                });
                break;
            case Units.DestructibleRockEx1HorizontalHuge:
                footprint.AddRange(new Vector2[]
                {
                    new(-5.5f,  1.5f), new(-4.5f,  1.5f), new(-3.5f,  1.5f), new(-2.5f,  1.5f), new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f), new(2.5f,  1.5f), new(3.5f,  1.5f), new(4.5f,  1.5f), new(5.5f,  1.5f),
                    new(-5.5f,  0.5f), new(-4.5f,  0.5f), new(-3.5f,  0.5f), new(-2.5f,  0.5f), new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f), new(2.5f,  0.5f), new(3.5f,  0.5f), new(4.5f,  0.5f), new(5.5f,  0.5f),
                    new(-5.5f, -0.5f), new(-4.5f, -0.5f), new(-3.5f, -0.5f), new(-2.5f, -0.5f), new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f), new(2.5f, -0.5f), new(3.5f, -0.5f), new(4.5f, -0.5f), new(5.5f, -0.5f),
                    new(-5.5f, -1.5f), new(-4.5f, -1.5f), new(-3.5f, -1.5f), new(-2.5f, -1.5f), new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f), new(2.5f, -1.5f), new(3.5f, -1.5f), new(4.5f, -1.5f), new(5.5f, -1.5f),
                });
                break;
            case Units.UnbuildablePlatesDestructible:
                // You can walk on it, no footprint (please I hope I never rely on this for placement, yikes)
                break;
            default:
                Logger.Warning("No footprint found for rock {0} at {1}", rock.Name, rock.Position);
                return GetGenericFootprint(rock);
        }

        return footprint.Select(cell => cell + rock.Position.ToVector2()).ToList();
    }

    private List<Vector2> GetGenericFootprint(Unit obstacle) {
        return _mapAnalyzer.BuildSearchGrid(obstacle.Position, (int)obstacle.Radius).Select(cell => cell.AsWorldGridCenter().ToVector2()).ToList();
    }
}
