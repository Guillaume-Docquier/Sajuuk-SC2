using System.Collections.Generic;
using System.Numerics;
using Sajuuk.MapAnalysis.RegionAnalysis;

namespace Sajuuk.Managers.ScoutManagement.ScoutingTasks;

public interface IScoutingTaskFactory {
    public ExpandScoutingTask CreateExpandScoutingTask(Vector2 scoutLocation, int priority, int maxScouts, bool waitForExpand = false);
    public MaintainVisibilityScoutingTask CreateMaintainVisibilityScoutingTask(IReadOnlyCollection<Vector2> area, int priority, int maxScouts);
    public RegionScoutingTask CreateRegionScoutingTask(IRegion region, int priority = 0, int maxScouts = 999);
}
