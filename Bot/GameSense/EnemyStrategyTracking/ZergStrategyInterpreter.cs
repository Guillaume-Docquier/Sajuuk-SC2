using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.EnemyStrategyTracking;

public class ZergStrategyInterpreter : IStrategyInterpreter {
    private bool _isInitialized = false;

    private ulong _poolTiming = 0;
    private ulong _expandTiming = 0;
    private ulong _zerglingAttackTiming = 0;

    private static ExpandLocation _enemyMain;
    private static ExpandLocation _enemyNatural;

    private static Region _enemyMainRegion;
    private static Region _enemyNaturalRegion;

    private readonly ulong _vulnerabilityWindow = TimeUtils.SecsToFrames(4 * 60);

    private static readonly ulong SpawningPoolBuildTime = (ulong)KnowledgeBase.GetUnitTypeData(Units.SpawningPool).BuildTime;
    private static readonly ulong ZerglingBuildTime = (ulong)KnowledgeBase.GetUnitTypeData(Units.Zergling).BuildTime;
    private static readonly ulong HatcheryBuildTime = (ulong)KnowledgeBase.GetUnitTypeData(Units.Hatchery).BuildTime;

    private static readonly float HatcheryRadius = KnowledgeBase.GetBuildingRadius(Units.Hatchery);

    private static readonly ulong TwelvePoolTiming = TimeUtils.SecsToFrames(15);
    private static readonly ulong TwelvePoolZerglingTiming = TimeUtils.SecsToFrames(1 * 60 + 19);
    private static readonly ulong SixteenHatchTiming = TimeUtils.SecsToFrames(45);
    private static readonly ulong AggressiveHatchGasPoolTiming = TimeUtils.SecsToFrames(60);
    private static readonly ulong HatchGasPoolTiming = TimeUtils.SecsToFrames(1 * 60 + 12);

    private static readonly ulong OneBaseTiming = TimeUtils.SecsToFrames(2 * 60 + 30);

    public EnemyStrategy Interpret(List<Unit> enemyUnits) {
        if (!ExpandAnalyzer.IsInitialized || !RegionAnalyzer.IsInitialized) {
            return EnemyStrategy.Unknown;
        }

        if (!_isInitialized) {
            Init();
        }

        AnalyzeTimings(enemyUnits);

        return InterpretTimings();
    }

    private void Init() {
        _enemyMain = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Main);
        _enemyNatural = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Natural);

        _enemyMainRegion = _enemyMain.Position.GetRegion();
        _enemyNaturalRegion = _enemyNatural.Position.GetRegion();

        _isInitialized = true;
    }

    // TODO GD We could split all of this and pop from a list of checks once done?
    private void AnalyzeTimings(IReadOnlyCollection<Unit> enemyUnits) {
        var enemyZerglings = enemyUnits.Where(unit => unit.UnitType == Units.Zergling).ToList();
        if (_poolTiming == 0) {
            var spawningPool = enemyUnits.FirstOrDefault(unit => unit.UnitType == Units.SpawningPool);
            if (spawningPool != null) {
                _poolTiming = Controller.Frame - (ulong)(SpawningPoolBuildTime * spawningPool.RawUnitData.BuildProgress);
            }
            else if (enemyZerglings.Count > 0) {
                _poolTiming = Controller.Frame - SpawningPoolBuildTime - ZerglingBuildTime; // TODO GD Consider distance to base?
            }
        }

        if (_expandTiming == 0) {
            var hatchery = enemyUnits
                .Where(unit => unit.UnitType == Units.Hatchery)
                .FirstOrDefault(hatchery => hatchery.DistanceTo(_enemyNatural.Position) <= HatcheryRadius);

            if (hatchery != null) {
                _expandTiming = Controller.Frame - (ulong)(HatcheryBuildTime * hatchery.RawUnitData.BuildProgress);
            }
        }

        if (_zerglingAttackTiming == 0) {
            if (enemyZerglings.Count(zergling => zergling.GetRegion() != _enemyMainRegion && zergling.GetRegion() != _enemyNaturalRegion) >= 6) {
                _zerglingAttackTiming = Controller.Frame;
            }
        }
    }

    /// <summary>
    /// Return the current estimated enemy strategy based on observed timings
    /// </summary>
    /// <returns>The best guess for the current enemy strategy</returns>
    private EnemyStrategy InterpretTimings() {
        if (_zerglingAttackTiming != 0 && _zerglingAttackTiming < _vulnerabilityWindow) {
            return EnemyStrategy.ZerglingRush;
        }

        if (IsTiming(TwelvePoolTiming, _poolTiming)) {
            return EnemyStrategy.TwelvePool;
        }

        if (IsTiming(AggressiveHatchGasPoolTiming, _poolTiming)) {
            return EnemyStrategy.AggressivePool;
        }

        if (IsTiming(SixteenHatchTiming, _expandTiming)) {
            return EnemyStrategy.SixteenHatch;
        }

        if (_expandTiming == 0 && Controller.Frame >= OneBaseTiming) {
            return EnemyStrategy.OneBase;
        }

        return EnemyStrategy.Unknown;
    }

    /// <summary>
    /// Determines if the actual timing matches the expected timing, given a tolerance in seconds
    /// </summary>
    /// <param name="expectedTiming">The expected timing</param>
    /// <param name="actualTiming">The actual timing</param>
    /// <param name="tolerance">The timing tolerance in seconds</param>
    /// <returns>True if the timings match, false otherwise</returns>
    private static bool IsTiming(ulong expectedTiming, ulong actualTiming, int tolerance = 10) {
        if (actualTiming == 0) {
            return false;
        }

        return actualTiming < expectedTiming + TimeUtils.SecsToFrames(tolerance);
    }
}
