using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense;

public class UnitsTracker : IUnitsTracker, INeedUpdating {
    private readonly IVisibilityTracker _visibilityTracker;
    private IUnitFactory _unitFactory;

    private const int EnemyDeathDelaySeconds = 4 * 60;

    private bool _isInitialized = false;

    public Dictionary<ulong, Unit> UnitsByTag { get; private set; } = new Dictionary<ulong, Unit>();
    public List<Unit> NewOwnedUnits { get; } = new List<Unit>();

    public List<Unit> NeutralUnits { get; private set; } = new List<Unit>();
    public List<Unit> OwnedUnits { get; private set; } = new List<Unit>();
    public List<Unit> EnemyUnits { get; private set; } = new List<Unit>();

    /// <summary>
    /// Holds all the units that we've lost vision of.
    /// Does not contain units that we confirmed have moved in the meantime.
    /// </summary>
    public Dictionary<ulong, Unit> EnemyGhostUnits { get; } = new Dictionary<ulong, Unit>();

    /// <summary>
    /// Holds all the units that are unaccounted for and that we know are not where we last saw them.
    /// </summary>
    // TODO GD Change EnemyMemorizedUnits to include all units that we know of (Units + Ghosts + Unaccounted for)
    public Dictionary<ulong, Unit> EnemyMemorizedUnits { get; } = new Dictionary<ulong, Unit>();

    public UnitsTracker(
        IVisibilityTracker visibilityTracker
    ) {
        _visibilityTracker = visibilityTracker;
    }

    public void WithUnitsFactory(IUnitFactory unitFactory) {
        _unitFactory = unitFactory;
    }

    /**
     * Returns all units of a certain type from the provided unitPool, including units of equivalent types.
     * Buildings that are in production are included.
     */
    public IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, uint unitTypeToGet) {
        return GetUnits(unitPool, new HashSet<uint>{ unitTypeToGet });
    }

    /**
     * Returns all units that match a certain set of types from the provided unitPool, including units of equivalent types.
     * Buildings that are in production are included.
     */
    public IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, HashSet<uint> unitTypesToGet, bool includeCloaked = false) {
        var equivalentUnitTypes = unitTypesToGet
            .Where(unitTypeToGet => Units.EquivalentTo.ContainsKey(unitTypeToGet))
            .SelectMany(unitTypeToGet => Units.EquivalentTo[unitTypeToGet])
            .ToList();

        unitTypesToGet.UnionWith(equivalentUnitTypes);

        foreach (var unit in unitPool) {
            if (!unitTypesToGet.Contains(unit.UnitType)) {
                continue;
            }

            if (unit.IsCloaked && !includeCloaked) {
                continue;
            }

            yield return unit;
        }
    }

    public List<Unit> GetUnits(Alliance alliance) {
        return alliance switch
        {
            Alliance.Self => OwnedUnits,
            Alliance.Enemy => EnemyUnits,
            Alliance.Neutral => NeutralUnits,
            _ => new List<Unit>()
        };
    }

    public List<Unit> GetGhostUnits(Alliance alliance) {
        return alliance switch
        {
            Alliance.Enemy => EnemyGhostUnits.Values.ToList(),
            _ => new List<Unit>()
        };
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        var unitsAsReportedByTheApi = observation.Observation.RawData.Units.ToList();
        var currentFrame = observation.Observation.GameLoop;

        if (!_isInitialized) {
            Init(unitsAsReportedByTheApi, currentFrame);
            LogUnknownNeutralUnits();

            return;
        }

        NewOwnedUnits.Clear();

        // Find new units and update existing ones
        unitsAsReportedByTheApi.ForEach(newRawUnit => {
            if (UnitsByTag.ContainsKey(newRawUnit.Tag)) {
                UnitsByTag[newRawUnit.Tag].Update(newRawUnit, currentFrame);
            }
            else {
                HandleNewUnit(newRawUnit, currentFrame);
            }
        });

        // Handle dead units
        var deadUnitTags = observation.Observation.RawData.Event?.DeadUnits?.ToHashSet() ?? new HashSet<ulong>();
        HandleDeadUnits(deadUnitTags, unitsAsReportedByTheApi, currentFrame);
        RememberEnemyUnitsOutOfSight(unitsAsReportedByTheApi);
        EraseGhosts();

        UpdateUnitLists();
    }

    private void Init(IEnumerable<SC2APIProtocol.Unit> rawUnits, ulong frame) {
        var units = rawUnits.Select(rawUnit => _unitFactory.CreateUnit(rawUnit, frame)).ToList();

        UnitsByTag = units.ToDictionary(unit => unit.Tag);

        OwnedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        NeutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        EnemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();

        _isInitialized = true;
    }

    private void LogUnknownNeutralUnits() {
        var unknownNeutralUnits = NeutralUnits.DistinctBy(unit => unit.UnitType)
            .Where(unit => !Units.Destructibles.Contains(unit.UnitType) && !Units.MineralFields.Contains(unit.UnitType) && !Units.GasGeysers.Contains(unit.UnitType) && unit.UnitType != Units.XelNagaTower)
            .Select(unit => (unit.Name, unit.UnitType))
            .ToList();

        Logger.Metric($"Unknown Neutral Units: [{string.Join(", ", unknownNeutralUnits)}]");
    }

    private void HandleNewUnit(SC2APIProtocol.Unit newRawUnit, ulong currentFrame) {
        var newUnit = _unitFactory.CreateUnit(newRawUnit, currentFrame);

        if (newUnit.Alliance == Alliance.Self) {
            Logger.Info("{0} was born", newUnit);
            NewOwnedUnits.Add(newUnit);
        }
        else {
            var equivalentUnit = UnitsByTag
                .Select(kv => kv.Value)
                .Where(unit => unit.UnitType == newUnit.UnitType)
                .Where(unit => unit.Alliance == newUnit.Alliance)
                .FirstOrDefault(unit => unit.Position.DistanceTo(newUnit.Position) <= 0.0001f); // Refinery snapshot can have a slightly different Z value

            // Buildings can have 2 units representing them: the snapshot version and the real version
            // The real version is only available when visible
            // The snapshot is only available when not visible
            if (equivalentUnit != default) {
                UnitsByTag.Remove(equivalentUnit.Tag);
                equivalentUnit.Update(newRawUnit, currentFrame);
                newUnit = equivalentUnit;
            }
            else if (newUnit.Alliance == Alliance.Enemy) {
                newUnit.DeathDelay = TimeUtils.SecsToFrames(EnemyDeathDelaySeconds);
                EnemyGhostUnits.Remove(newUnit.Tag);
                EnemyMemorizedUnits.Remove(newUnit.Tag);
            }
        }

        UnitsByTag[newUnit.Tag] = newUnit;
    }

    private void HandleDeadUnits(IReadOnlySet<ulong> deadUnitTags, List<SC2APIProtocol.Unit> currentlyVisibleUnits, uint currentFrame) {
        if (deadUnitTags == null) {
            return;
        }

        foreach (var unit in UnitsByTag.Select(unit => unit.Value).ToList()) {
            // We use unit.IsDead(currentFrame) as a fallback for cases where we missed a frame
            // Also, drones that morph into buildings are not considered 'killed' and won't be present in deadUnitIds
            if (deadUnitTags.Contains(unit.Tag) || unit.IsDead(currentFrame)) {
                unit.Died();

                UnitsByTag.Remove(unit.Tag);
                EnemyGhostUnits.Remove(unit.Tag);
                EnemyMemorizedUnits.Remove(unit.Tag);
            }
        }

        // Terran buildings can move, we'll consider them dead if we don't know where they are
        // We should add them to the memorized units, probably
        var visibleUnitTags = currentlyVisibleUnits.Select(unit => unit.Tag).ToHashSet();
        var buildingsThatProbablyMoved = UnitsByTag.Values
            .Where(unit => unit.Alliance == Alliance.Enemy)
            .Where(enemy => Units.Buildings.Contains(enemy.UnitType))
            .Where(enemyBuilding => !visibleUnitTags.Contains(enemyBuilding.Tag))
            .Where(_visibilityTracker.IsVisible);

        foreach (var buildingThatProbablyMoved in buildingsThatProbablyMoved) {
            buildingThatProbablyMoved.Died();

            UnitsByTag.Remove(buildingThatProbablyMoved.Tag);
        }
    }

    private void RememberEnemyUnitsOutOfSight(List<SC2APIProtocol.Unit> currentlyVisibleUnits) {
        var enemyUnitIdsNotInVision = UnitsByTag.Values
            .Where(unit => unit.Alliance == Alliance.Enemy)
            .Where(enemyUnit => !Units.Buildings.Contains(enemyUnit.UnitType))
            .Select(enemyUnit => enemyUnit.Tag)
            .Except(currentlyVisibleUnits.Select(unit => unit.Tag))
            .ToHashSet();

        var enemyUnitsNotInVision = UnitsByTag.Values.Where(unit => enemyUnitIdsNotInVision.Contains(unit.Tag)).ToList();
        foreach (var enemyUnit in enemyUnitsNotInVision) {
            UnitsByTag.Remove(enemyUnit.Tag);

            if (!_visibilityTracker.IsVisible(enemyUnit)) {
                if (EnemyGhostUnits.ContainsKey(enemyUnit.Tag)) {
                    Logger.Warning("Trying to add an enemy {0} to the ghosts, but it is already present", enemyUnit);
                }

                EnemyGhostUnits[enemyUnit.Tag] = enemyUnit;
            }

            if (EnemyMemorizedUnits.ContainsKey(enemyUnit.Tag)) {
                Logger.Warning("Trying to add an enemy {0} to the memorized units, but it is already present", enemyUnit);
            }

            EnemyMemorizedUnits[enemyUnit.Tag] = enemyUnit;
        }
    }

    private void EraseGhosts() {
        foreach (var (ghostEnemyUnitId, ghostEnemyUnit) in EnemyGhostUnits) {
            if (_visibilityTracker.IsVisible(ghostEnemyUnit)) {
                EnemyGhostUnits.Remove(ghostEnemyUnitId);
            }
        }
    }

    private void UpdateUnitLists() {
        OwnedUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Self).Select(unit => unit.Value).ToList();
        NeutralUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Neutral).Select(unit => unit.Value).ToList();
        EnemyUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Enemy).Select(unit => unit.Value).ToList();
    }
}
