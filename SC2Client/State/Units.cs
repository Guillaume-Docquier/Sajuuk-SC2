using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client.GameData;

namespace SC2Client.State;

public class Units : IUnits {
    /// <summary>
    /// Workers disappear when going inside extractors for 1.415 seconds
    /// We'll change their death delay so that we don't think they're dead
    /// </summary>
    private static readonly ulong GasDeathDelay = TimeUtils.SecsToFrames(1.415f) + 5; // +5 just to be sure

    /// <summary>
    /// We delay the death of enemy units because if they die out of sight, we'll never know.
    /// </summary>
    private const int EnemyDeathDelaySeconds = 4 * 60;

    private readonly ILogger _logger;
    private readonly KnowledgeBase _knowledgeBase;

    private readonly Dictionary<ulong, Unit> _unitsByTag = new Dictionary<ulong, Unit>();
    private List<Unit> _neutralUnits = new List<Unit>();
    private List<Unit> _ownedUnits = new List<Unit>();
    private List<Unit> _enemyUnits = new List<Unit>();

    public IReadOnlyList<IUnit> NeutralUnits => _neutralUnits;
    public IReadOnlyList<IUnit> OwnedUnits => _ownedUnits;
    public IReadOnlyList<IUnit> EnemyUnits => _enemyUnits;

    public Units(ILogger logger, KnowledgeBase knowledgeBase, ResponseObservation observation) {
        _logger = logger;
        _knowledgeBase = knowledgeBase;

        Update(observation);
        LogUnknownNeutralUnits();
    }

    public void Update(ResponseObservation observation) {
        var rawUnits = observation.Observation.RawData.Units.ToList();
        var currentFrame = observation.Observation.GameLoop;

        // Find new units and update existing ones
        foreach (var rawUnit in rawUnits) {
            if (_unitsByTag.TryGetValue(rawUnit.Tag, out var unit)) {
                unit.Update(rawUnit, currentFrame, _knowledgeBase);
            }
            else {
                HandleNewUnit(rawUnit, currentFrame);
            }
        }

        // Handle dead units
        var deadUnitTags = observation.Observation.RawData.Event?.DeadUnits?.ToHashSet() ?? new HashSet<ulong>();
        HandleDeadUnits(deadUnitTags, rawUnits, currentFrame);

        // Finalize state
        var units = _unitsByTag.Values.ToList(); // TODO GD Benchmark if a groupby would be faster
        _ownedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        _neutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        _enemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="newRawUnit"></param>
    /// <param name="currentFrame"></param>
    private void HandleNewUnit(SC2APIProtocol.Unit newRawUnit, ulong currentFrame) {
        var newUnit = new Unit(newRawUnit, currentFrame, _knowledgeBase);

        if (newUnit.Alliance == Alliance.Self) {
            _logger.Info($"{newUnit} was born");
        }
        else {
            var equivalentUnit = _unitsByTag
                .Select(kv => kv.Value)
                .Where(unit => unit.UnitType == newUnit.UnitType)
                .Where(unit => unit.Alliance == newUnit.Alliance)
                .FirstOrDefault(unit => unit.Position.DistanceTo(newUnit.Position) <= 0.0001f); // Refinery snapshot can have a slightly different Z value

            // Buildings can have 2 units representing them: the snapshot version and the real version
            // The real version is only available when visible
            // The snapshot is only available when not visible
            if (equivalentUnit != default) {
                _unitsByTag.Remove(equivalentUnit.Tag);
                equivalentUnit.Update(newRawUnit, currentFrame, _knowledgeBase);
                newUnit = equivalentUnit;
            }
        }

        _unitsByTag[newUnit.Tag] = newUnit;
    }

    /// <summary>
    /// Detects dead units and updates unit collections.
    ///
    /// Terran buildings can lift and move, we'll consider them dead when they do.
    ///
    /// Larvae that become eggs don't die, their unit type changes and their id is stable.
    /// Drones that morph into buildings are not considered 'killed' by the api but they cease to exist.
    /// Eggs die when their unit is born.
    ///
    /// TODO GD We should differentiate the type of death. Killed vs ceased to exist have different use cases.
    /// </summary>
    /// <param name="deadUnitTags">Dead unit tags as reported by the API.</param>
    /// <param name="currentlyVisibleRawUnits">Units that we currently see.</param>
    /// <param name="currentFrame">The current frame number.</param>
    private void HandleDeadUnits(IReadOnlySet<ulong> deadUnitTags, IEnumerable<SC2APIProtocol.Unit> currentlyVisibleRawUnits, uint currentFrame) {
        var units = _unitsByTag.Select(unit => unit.Value).ToList();
        var extractors = UnitQueries.GetUnits(units.Where(unit => unit.Alliance == Alliance.Self), UnitTypeId.Extractors).ToList();
        foreach (var unit in units) {
            if (deadUnitTags.Contains(unit.Tag) || IsDead(unit, currentFrame, extractors)) {
                _logger.Info($"{unit} died");
                _unitsByTag.Remove(unit.Tag);
            }
        }

        // Terran buildings can move, we'll consider them dead if we don't know where they are
        var visibleUnitTags = currentlyVisibleRawUnits.Select(unit => unit.Tag).ToHashSet();
        var enemyBuildingsThatProbablyMoved = _unitsByTag.Values
            .Where(unit => unit.Alliance == Alliance.Enemy)
            .Where(enemy => UnitTypeId.Buildings.Contains(enemy.UnitType))
            .Where(enemyBuilding => !visibleUnitTags.Contains(enemyBuilding.Tag))
            .Where(_visibilityTracker.IsVisible);

        foreach (var enemyBuildingThatProbablyMoved in enemyBuildingsThatProbablyMoved) {
            _unitsByTag.Remove(enemyBuildingThatProbablyMoved.Tag);
        }
    }

    /// <summary>
    /// Determines if a unit is dead based on when it was last seen.
    /// Sometimes the API is finicky and doesn't report deaths correctly.
    ///
    /// We'll give a special treatment to workers near gas extractors because when they get in, they just vanish from the api.
    ///
    /// We keep enemy units for a while because going out of vision is normal.
    /// We consider them dead after a delay otherwise if they die out of sight we'll never know.
    /// </summary>
    /// <param name="unit">The unit that might be dead.</param>
    /// <param name="currentFrame">The current frame number.</param>
    /// <param name="selfGasExtractors">The list of existing gas extractors owned by the player.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">If the unit's alliance is unexpected.</exception>
    private bool IsDead(IUnit unit, ulong currentFrame, IEnumerable<IUnit> selfGasExtractors) {
        switch (unit.Alliance) {
            case Alliance.Self when UnitTypeId.Workers.Contains(unit.UnitType) && selfGasExtractors.Any(extractor => AreUnitsTouching(extractor, unit)):
                return unit.LastSeen + GasDeathDelay < currentFrame;
            case Alliance.Self:
                return unit.LastSeen < currentFrame;
            case Alliance.Ally:
            case Alliance.Neutral:
            case Alliance.Enemy:
                return unit.LastSeen + EnemyDeathDelaySeconds < currentFrame;
            default:
                throw new ArgumentOutOfRangeException(unit.Alliance.ToString());
        }
    }

    /// <summary>
    /// Logs the name and unit type of unknown neutral units.
    /// This is just in case we forget to register some mineral fields or rocks.
    /// </summary>
    private void LogUnknownNeutralUnits() {
        var unknownNeutralUnits = NeutralUnits.DistinctBy(unit => unit.UnitType)
            .Where(unit => !UnitTypeId.Destructibles.Contains(unit.UnitType) && !UnitTypeId.MineralFields.Contains(unit.UnitType) && !UnitTypeId.GasGeysers.Contains(unit.UnitType) && unit.UnitType != UnitTypeId.XelNagaTower)
            .Select(unit => (unit.Name, unit.UnitType))
            .ToList();

        _logger.Metric($"Unknown Neutral Units: [{string.Join(", ", unknownNeutralUnits)}]");
    }

    /// <summary>
    /// Determines if 2 units are touching.
    /// </summary>
    /// <param name="first">The first unit.</param>
    /// <param name="second">The second unit.</param>
    /// <returns></returns>
    private bool AreUnitsTouching(IUnit first, IUnit second) {
        return first.Position.DistanceTo(second.Position) <= first.Radius + second.Radius + 0.15; // +0.15 for safety
    }
}
