using System.Numerics;
using Algorithms.ExtensionMethods;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.State;

namespace SC2Client;

/// <summary>
/// Calculates the footprints of various game units.
/// TODO GD As an optimization, well known objects should be cached instead of created on every call.
/// </summary>
public class FootprintCalculator {
    private readonly KnowledgeBase _knowledgeBase;
    private readonly ILogger _logger;

    public FootprintCalculator(
        KnowledgeBase knowledgeBase,
        ILogger logger
    ) {
        _knowledgeBase = knowledgeBase;
        _logger = logger.CreateNamed("FootprintCalculator");
    }

    public IEnumerable<Vector2> GetFootprint(Vector2 buildingCenter, uint buildingType) {
        return buildingCenter.BuildSearchGrid((int)_knowledgeBase.GetBuildingRadius(buildingType));
    }

    public List<Vector2> GetFootprint(IUnit obstacle) {
        if (UnitTypeId.MineralFields.Contains(obstacle.UnitType)) {
            return GetMineralFootprint(obstacle);
        }

        if (UnitTypeId.Obstacles.Contains(obstacle.UnitType)) {
            return GetRockFootprint(obstacle);
        }

        return GetGenericFootprint(obstacle);
    }

    /// <summary>
    /// Mineral fields are 1x2 and their position is in between the two cells.
    /// So the mineral:
    /// ┌───────┐
    /// │   .   │
    /// └───────┘
    /// Becomes:
    /// ┌───┐┌───┐
    /// │ . ││ . │
    /// └───┘└───┘
    /// </summary>
    /// <param name="mineral"></param>
    /// <returns></returns>
    private static List<Vector2> GetMineralFootprint(IUnit mineral) {
        return new List<Vector2>
        {
            mineral.Position.Translate(xTranslation: -0.5f).AsWorldGridCenter().ToVector2(),
            mineral.Position.Translate(xTranslation: 0.5f).AsWorldGridCenter().ToVector2(),
        };
    }

    private List<Vector2> GetRockFootprint(IUnit rock) {
        var footprint = new List<Vector2>();
        switch (rock.UnitType) {
            case UnitTypeId.DestructibleDebris4x4:
            case UnitTypeId.DestructibleRock4x4:
            case UnitTypeId.DestructibleRockEx14x4:
                footprint.AddRange(new Vector2[]
                {
                    new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f),
                    new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f),
                    new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f),
                    new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f),
                });
                break;
            case UnitTypeId.DestructibleCityDebris6x6:
            case UnitTypeId.DestructibleDebris6x6:
            case UnitTypeId.DestructibleRock6x6:
            case UnitTypeId.DestructibleRockEx16x6:
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
            case UnitTypeId.DestructibleRampDiagonalHugeBLUR:
            case UnitTypeId.DestructibleDebrisRampDiagonalHugeBLUR:
            case UnitTypeId.DestructibleCityDebrisHugeDiagonalBLUR:
            case UnitTypeId.DestructibleRockEx1DiagonalHugeBLUR:
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
            case UnitTypeId.DestructibleRampDiagonalHugeULBR:
            case UnitTypeId.DestructibleDebrisRampDiagonalHugeULBR:
            case UnitTypeId.DestructibleRockEx1DiagonalHugeULBR:
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
            case UnitTypeId.DestructibleRampVerticalHuge:
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
            case UnitTypeId.DestructibleRockEx1HorizontalHuge:
                footprint.AddRange(new Vector2[]
                {
                    new(-5.5f,  1.5f), new(-4.5f,  1.5f), new(-3.5f,  1.5f), new(-2.5f,  1.5f), new(-1.5f,  1.5f), new(-0.5f,  1.5f), new(0.5f,  1.5f), new(1.5f,  1.5f), new(2.5f,  1.5f), new(3.5f,  1.5f), new(4.5f,  1.5f), new(5.5f,  1.5f),
                    new(-5.5f,  0.5f), new(-4.5f,  0.5f), new(-3.5f,  0.5f), new(-2.5f,  0.5f), new(-1.5f,  0.5f), new(-0.5f,  0.5f), new(0.5f,  0.5f), new(1.5f,  0.5f), new(2.5f,  0.5f), new(3.5f,  0.5f), new(4.5f,  0.5f), new(5.5f,  0.5f),
                    new(-5.5f, -0.5f), new(-4.5f, -0.5f), new(-3.5f, -0.5f), new(-2.5f, -0.5f), new(-1.5f, -0.5f), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(1.5f, -0.5f), new(2.5f, -0.5f), new(3.5f, -0.5f), new(4.5f, -0.5f), new(5.5f, -0.5f),
                    new(-5.5f, -1.5f), new(-4.5f, -1.5f), new(-3.5f, -1.5f), new(-2.5f, -1.5f), new(-1.5f, -1.5f), new(-0.5f, -1.5f), new(0.5f, -1.5f), new(1.5f, -1.5f), new(2.5f, -1.5f), new(3.5f, -1.5f), new(4.5f, -1.5f), new(5.5f, -1.5f),
                });
                break;
            case UnitTypeId.UnbuildablePlatesDestructible:
                // You can walk on it, no footprint (please I hope I never rely on this for placement, yikes)
                break;
            default:
                _logger.Warning($"No footprint found for rock {rock.Name}[{rock.UnitType}] at {rock.Position}");
                return GetGenericFootprint(rock);
        }

        return footprint.Select(cell => cell + rock.Position.ToVector2()).ToList();
    }

    private static List<Vector2> GetGenericFootprint(IUnit obstacle) {
        return obstacle.Position.ToVector2().BuildSearchGrid((int)obstacle.Radius)
            .Select(cell => cell.AsWorldGridCenter())
            .ToList();
    }
}
