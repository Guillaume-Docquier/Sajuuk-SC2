using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.MapAnalysis.RegionAnalysis;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.EnemyStrategyTracking.StrategyInterpretation;

public class ZergStrategyInterpreter : IStrategyInterpreter {
    private readonly IFrameClock _frameClock;
    private readonly IRegionsTracker _regionsTracker;

    private bool _isInitialized = false;

    private ulong _poolTiming = 0;
    private ulong _expandTiming = 0;
    private ulong _zerglingAttackTiming = 0;

    private static IExpandLocation _enemyMain;
    private static IExpandLocation _enemyNatural;

    private static IRegion _enemyMainRegion;
    private static IRegion _enemyNaturalRegion;

    private readonly ulong _vulnerabilityWindow = TimeUtils.SecsToFrames(4 * 60);

    private readonly ulong _spawningPoolBuildTime;
    private readonly ulong _zerglingBuildTime;
    private readonly ulong _hatcheryBuildTime;

    private readonly float _hatcheryRadius;

    private static readonly ulong TwelvePoolTiming = TimeUtils.SecsToFrames(15);
    private static readonly ulong TwelvePoolZerglingTiming = TimeUtils.SecsToFrames(1 * 60 + 19);
    private static readonly ulong SixteenHatchTiming = TimeUtils.SecsToFrames(45);
    private static readonly ulong AggressiveHatchGasPoolTiming = TimeUtils.SecsToFrames(60);
    private static readonly ulong HatchGasPoolTiming = TimeUtils.SecsToFrames(1 * 60 + 12);

    private static readonly ulong OneBaseTiming = TimeUtils.SecsToFrames(2 * 60 + 30);

    public ZergStrategyInterpreter(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IRegionsTracker regionsTracker
    ) {
        _frameClock = frameClock;
        _regionsTracker = regionsTracker;

        _spawningPoolBuildTime = (ulong)knowledgeBase.GetUnitTypeData(Units.SpawningPool).BuildTime;
        _zerglingBuildTime = (ulong)knowledgeBase.GetUnitTypeData(Units.Zergling).BuildTime;
        _hatcheryBuildTime = (ulong)knowledgeBase.GetUnitTypeData(Units.Hatchery).BuildTime;
        _hatcheryRadius = knowledgeBase.GetBuildingRadius(Units.Hatchery);
    }

    public EnemyStrategy Interpret(List<Unit> enemyUnits) {
        if (!_isInitialized) {
            Init();
        }

        AnalyzeTimings(enemyUnits);

        return InterpretTimings();
    }

    private void Init() {
        _enemyMain = _regionsTracker.GetExpand(Alliance.Enemy, ExpandType.Main);
        _enemyNatural = _regionsTracker.GetExpand(Alliance.Enemy, ExpandType.Natural);

        _enemyMainRegion = _regionsTracker.GetRegion(_enemyMain.Position);
        _enemyNaturalRegion = _regionsTracker.GetRegion(_enemyNatural.Position);

        _isInitialized = true;
    }

    // TODO GD We could split all of this and pop from a list of checks once done?
    private void AnalyzeTimings(IReadOnlyCollection<Unit> enemyUnits) {
        var enemyZerglings = enemyUnits.Where(unit => unit.UnitType == Units.Zergling).ToList();
        if (_poolTiming == 0) {
            var spawningPool = enemyUnits.FirstOrDefault(unit => unit.UnitType == Units.SpawningPool);
            if (spawningPool != null) {
                _poolTiming = _frameClock.CurrentFrame - (ulong)(_spawningPoolBuildTime * spawningPool.RawUnitData.BuildProgress);
            }
            else if (enemyZerglings.Count > 0) {
                _poolTiming = _frameClock.CurrentFrame - _spawningPoolBuildTime - _zerglingBuildTime; // TODO GD Consider distance to base?
            }
        }

        if (_expandTiming == 0) {
            var hatchery = enemyUnits
                .Where(unit => unit.UnitType == Units.Hatchery)
                .FirstOrDefault(hatchery => hatchery.DistanceTo(_enemyNatural.Position) <= _hatcheryRadius);

            if (hatchery != null) {
                _expandTiming = _frameClock.CurrentFrame - (ulong)(_hatcheryBuildTime * hatchery.RawUnitData.BuildProgress);
            }
        }

        if (_zerglingAttackTiming == 0) {
            if (enemyZerglings.Count(zergling => zergling.GetRegion() != _enemyMainRegion && zergling.GetRegion() != _enemyNaturalRegion) >= 6) {
                _zerglingAttackTiming = _frameClock.CurrentFrame;
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

        if (_expandTiming == 0 && _frameClock.CurrentFrame >= OneBaseTiming) {
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
