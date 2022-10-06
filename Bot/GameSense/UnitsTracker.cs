using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using SC2APIProtocol;

namespace Bot.GameSense;

public class UnitsTracker: INeedUpdating {
    public static readonly UnitsTracker Instance = new UnitsTracker();

    private bool _isInitialized = false;
    public static Dictionary<ulong, Unit> UnitsByTag;

    public static readonly List<Unit> NewOwnedUnits = new List<Unit>();

    public static List<Unit> NeutralUnits { get; private set; }
    public static List<Unit> OwnedUnits { get; private set; }
    public static List<Unit> EnemyUnits { get; private set; }

    public static HashSet<Unit> GhostEnemyUnits { get; } = new HashSet<Unit>();
    public static HashSet<Unit> MemoryEnemyUnits { get; } = new HashSet<Unit>();

    private const int EnemyDeathDelaySeconds = 5 * 60;

    private UnitsTracker() {}

    public void Reset() {
        _isInitialized = false;

        UnitsByTag = null;

        NewOwnedUnits.Clear();

        NeutralUnits = null;
        OwnedUnits = null;
        EnemyUnits = null;
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
        var deadUnitIds = observation.Observation.RawData.Event?.DeadUnits?.ToHashSet() ?? new HashSet<ulong>();
        HandleDeadUnits(deadUnitIds, currentFrame);
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
        else if (newUnit.Alliance == Alliance.Neutral) {
            var equivalentUnit = UnitsByTag
                .Select(kv => kv.Value)
                .FirstOrDefault(unit => unit.Position == newUnit.Position);

            // Resources have 2 units representing them: the snapshot version and the real version
            // The real version is only available when visible
            // The snapshot is only available when not visible
            if (equivalentUnit != default) {
                UnitsByTag.Remove(equivalentUnit.Tag);
                equivalentUnit.Update(newRawUnit, currentFrame);
                newUnit = equivalentUnit;
            }
        }
        else if (newUnit.Alliance == Alliance.Enemy) {
            newUnit.DeathDelay = Controller.SecsToFrames(EnemyDeathDelaySeconds);
        }

        UnitsByTag[newUnit.Tag] = newUnit;
    }

    private static void HandleDeadUnits(IReadOnlySet<ulong> deadUnitIds, uint currentFrame) {
        foreach (var unit in UnitsByTag.Select(unit => unit.Value).ToList()) {
            // We use unit.IsDead(currentFrame) as a fallback for cases where we missed a frame
            // Also, drones that morph into buildings are not considered 'killed' and won't be present in deadUnitIds
            if (deadUnitIds.Contains(unit.Tag) || unit.IsDead(currentFrame)) {
                unit.Died();

                UnitsByTag.Remove(unit.Tag);
                GhostEnemyUnits.Remove(unit);
                MemoryEnemyUnits.Remove(unit);
            }
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

            if (!VisibilityTracker.IsVisible(enemyUnit.Position)) {
                GhostEnemyUnits.Add(enemyUnit);
            }

            MemoryEnemyUnits.Add(enemyUnit);
        }
    }

    private static void EraseGhosts() {
        foreach (var ghostEnemyUnit in GhostEnemyUnits) {
            if (VisibilityTracker.IsVisible(ghostEnemyUnit.Position)) {
                GhostEnemyUnits.Remove(ghostEnemyUnit);
            }
        }
    }

    private static void UpdateUnitLists() {
        OwnedUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Self).Select(unit => unit.Value).ToList();
        NeutralUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Neutral).Select(unit => unit.Value).ToList();
        EnemyUnits = UnitsByTag.Where(unit => unit.Value.Alliance == Alliance.Enemy).Select(unit => unit.Value).ToList();
    }
}
