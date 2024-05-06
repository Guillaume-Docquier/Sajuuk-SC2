using SC2APIProtocol;
using SC2Client.GameData;

namespace SC2Client.State;

public class Units : IUnits {
    private readonly ILogger _logger;
    private readonly KnowledgeBase _knowledgeBase;

    private HashSet<ulong> _deadUnitTags = new HashSet<ulong>();
    private List<Unit> _neutralUnits = new List<Unit>();
    private List<Unit> _ownedUnits = new List<Unit>();
    private List<Unit> _enemyUnits = new List<Unit>();

    public IReadOnlySet<ulong> DeadUnitTags => _deadUnitTags;
    public IReadOnlyList<IUnit> NeutralUnits => _neutralUnits;
    public IReadOnlyList<IUnit> OwnedUnits => _ownedUnits;
    public IReadOnlyList<IUnit> EnemyUnits => _enemyUnits;

    public Units(ILogger logger, KnowledgeBase knowledgeBase, ResponseObservation observation) {
        _logger = logger;
        _knowledgeBase = knowledgeBase;

        Update(observation);
        LogUnknownNeutralUnits();
    }

    /// <summary>
    /// Updates the Units based on the latest game state observation.
    /// </summary>
    /// <param name="observation"></param>
    public void Update(ResponseObservation observation) {
        _deadUnitTags = observation.Observation.RawData.Event?.DeadUnits?.ToHashSet() ?? new HashSet<ulong>();

        var currentFrame = observation.Observation.GameLoop;
        var units = observation.Observation.RawData.Units
            .Select(rawUnit => new Unit(_knowledgeBase, currentFrame, rawUnit))
            .ToList();

        _ownedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        _neutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        _enemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();
    }

    /// <summary>
    /// Logs the name and unit type of unknown neutral units.
    /// This is just in case we forget to register some mineral fields or rocks.
    /// </summary>
    private void LogUnknownNeutralUnits() {
        var unknownNeutralUnits = NeutralUnits.DistinctBy(unit => unit.UnitType)
            .Where(unit => !UnitTypeId.Destructibles.Contains(unit.UnitType) &&
                           !UnitTypeId.MineralFields.Contains(unit.UnitType) &&
                           !UnitTypeId.GasGeysers.Contains(unit.UnitType) &&
                           unit.UnitType != UnitTypeId.XelNagaTower)
            .Select(unit => (unit.Name, unit.UnitType))
            .ToList();

        _logger.Metric($"Unknown Neutral Units: [{string.Join(", ", unknownNeutralUnits)}]");
    }
}
