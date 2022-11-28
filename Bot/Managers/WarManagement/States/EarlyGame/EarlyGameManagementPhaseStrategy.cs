using System;
using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameManagementPhaseStrategy : WarManagerStrategy {
    private readonly ArmySupervisor _defenseSupervisor = new ArmySupervisor();
    private readonly List<Region> _startingRegions;

    public EarlyGameManagementPhaseStrategy(WarManager context) : base(context) {
        var main = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Main).Position.GetRegion();
        var natural = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Natural).Position.GetRegion();
        _startingRegions = Pathfinder.FindPath(main, natural);
    }

    public override void Execute() {
        var regionToDefend = _startingRegions.MaxBy(region => RegionTracker.GetForce(region, Alliance.Enemy))!;
        _defenseSupervisor.AssignTarget(regionToDefend.Center, ApproximateRegionRadius(regionToDefend), canHuntTheEnemy: false);
        _defenseSupervisor.OnFrame();
    }

    public override bool CleanUp() {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Approximates the region's radius based on it's cells
    /// TODO GD This is a placeholder while we have a smarter region defense behaviour
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    private static float ApproximateRegionRadius(Region region) {
        return (float)Math.Sqrt(region.Cells.Count) / 2;
    }
}
