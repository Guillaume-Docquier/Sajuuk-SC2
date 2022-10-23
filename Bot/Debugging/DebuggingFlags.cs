using System.Collections.Generic;
using System.Linq;

namespace Bot.Debugging;

public static class DebuggingFlags {
    public static HashSet<string> GetAll() {
        return typeof(DebuggingFlags).GetFields().Select(x => x.GetValue(null)).Cast<string>().ToHashSet();
    }

    /// <summary>
    /// Enables displaying information about debugging flags
    /// </summary>
    public const string Help = ".help";

    /// <summary>
    /// Resets the debugging flags to their default
    /// </summary>
    public const string Reset = ".reset";

    /// <summary>
    /// Enables displaying build order data on screen
    /// </summary>
    public const string BuildOrder = ".build";

    /// <summary>
    /// Enables displaying detection range of enemy detectors
    /// </summary>
    public const string EnemyDetectors = ".detectors";

    /// <summary>
    /// Enables displaying individual walkable tiles
    /// </summary>
    public const string WalkableAreas = ".walkable";

    /// <summary>
    /// Enables displaying information on neutral destructible objects
    /// </summary>
    public const string NeutralUnits = ".neutral";

    /// <summary>
    /// Enables displaying income information on screen
    /// </summary>
    public const string IncomeRate = ".income";

    /// <summary>
    /// Enables displaying ghost units data
    /// </summary>
    public const string GhostUnits = ".ghost";

    /// <summary>
    /// Enables displaying enemy units data
    /// </summary>
    public const string KnownEnemyUnits = ".enemy";

    /// <summary>
    /// Enables displaying matchup data, like enemy race, % of map visible/explored
    /// </summary>
    public const string MatchupData = ".matchup";

    /// <summary>
    /// Enables displaying region data
    /// </summary>
    public const string Regions = ".regions";

    /// <summary>
    /// Enables displaying each cell's region
    /// </summary>
    public const string CellRegions = ".cellRegion";

    /// <summary>
    /// Enables displaying the choke points
    /// </summary>
    public const string ChokePoints = ".choke";

    /// <summary>
    /// Enables displaying the unexplored tiles
    /// </summary>
    public const string Exploration = ".explored";
}
