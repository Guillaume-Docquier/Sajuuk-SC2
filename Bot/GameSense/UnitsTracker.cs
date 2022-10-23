using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using SC2APIProtocol;

namespace Bot.GameSense;

public class UnitsTracker: INeedUpdating {
    public static readonly UnitsTracker Instance = new UnitsTracker();

    private bool _isInitialized = false;

    public static Dictionary<ulong, Unit> UnitsByTag { get; private set; } = new Dictionary<ulong, Unit>();

    public static readonly List<Unit> NewOwnedUnits = new List<Unit>();

    public static List<Unit> NeutralUnits { get; private set; } = new List<Unit>();
    public static List<Unit> OwnedUnits { get; private set; } = new List<Unit>();
    public static List<Unit> EnemyUnits { get; private set; } = new List<Unit>();

    public static Dictionary<ulong, Unit> EnemyGhostUnits { get; } = new Dictionary<ulong, Unit>();
    public static Dictionary<ulong, Unit> EnemyMemorizedUnits { get; } = new Dictionary<ulong, Unit>();

    private const int EnemyDeathDelaySeconds = 4 * 60;

    private UnitsTracker() {}

    public void Reset() {
        _isInitialized = false;

        UnitsByTag.Clear();

        NewOwnedUnits.Clear();

        NeutralUnits.Clear();
        OwnedUnits.Clear();
        EnemyUnits.Clear();
    }

    public void Update(ResponseObservation observation) {
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
        var units = rawUnits.Select(rawUnit => new Unit(rawUnit, frame)).ToList();

        UnitsByTag = units.ToDictionary(unit => unit.Tag);

        OwnedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        NeutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        EnemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();

        _isInitialized = true;
    }

    private static void LogUnknownNeutralUnits() {
        var unknownNeutralUnits = NeutralUnits.DistinctBy(unit => unit.UnitType)
            .Where(unit => !Units.Destructibles.Contains(unit.UnitType) && !Units.MineralFields.Contains(unit.UnitType) && !Units.GasGeysers.Contains(unit.UnitType) && unit.UnitType != Units.XelNagaTower)
            .Select(unit => (unit.Name, unit.UnitType))
            .ToList();

        Logger.Metric("Unknown Neutral Units: [{0}]", string.Join(", ", unknownNeutralUnits));
    }

    private static void HandleNewUnit(SC2APIProtocol.Unit newRawUnit, ulong currentFrame) {
        var newUnit = new Unit(newRawUnit, currentFrame);

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
                newUnit.DeathDelay = Controller.SecsToFrames(EnemyDeathDelaySeconds);
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
            .Where(VisibilityTracker.IsVisible);

        foreach (var buildingThatProbablyMoved in buildingsThatProbablyMoved) {
            buildingThatProbablyMoved.Died();

            UnitsByTag.Remove(buildingThatProbablyMoved.Tag);
        }
    }

    private static void RememberEnemyUnitsOutOfSight(List<SC2APIProtocol.Unit> currentlyVisibleUnits) {
        var enemyUnitIdsNotInVision = UnitsByTag.Values
            .Where(unit => unit.Alliance == Alliance.Enemy)
            .Where(enemyUnit => !Units.Buildings.Contains(enemyUnit.UnitType))
            .Select(enemyUnit => enemyUnit.Tag)
            .Except(currentlyVisibleUnits.Select(unit => unit.Tag))
            .ToHashSet();

        var enemyUnitsNotInVision = UnitsByTag.Values.Where(unit => enemyUnitIdsNotInVision.Contains(unit.Tag)).ToList();
        foreach (var enemyUnit in enemyUnitsNotInVision) {
            UnitsByTag.Remove(enemyUnit.Tag);

            if (!VisibilityTracker.IsVisible(enemyUnit)) {
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

    private static void EraseGhosts() {
        foreach (var (ghostEnemyUnitId, ghostEnemyUnit) in EnemyGhostUnits) {
            if (VisibilityTracker.IsVisible(ghostEnemyUnit)) {
                EnemyGhostUnits.Remove(ghostEnemyUnitId);
            }
        }
    }

    private static void UpdateUnitLists() {
        OwnedUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Self).Select(unit => unit.Value).ToList();
        NeutralUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Neutral).Select(unit => unit.Value).ToList();
        EnemyUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Enemy).Select(unit => unit.Value).ToList();
    }
}
