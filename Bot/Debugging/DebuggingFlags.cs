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
    /// Enables displaying build order data on screen
    /// </summary>
    public const string BuildOrder = ".build";

    /// <summary>
    /// Enables displaying detection range of enemy detectors
    /// </summary>
    public const string EnemyDetectors = ".detectors";

    /// <summary>
    /// Enables displaying individual unwalkable tiles
    /// </summary>
    public const string UnwalkableAreas = ".unwalkable";

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
    /// Enables displaying each region cells
    /// </summary>
    public const string RegionCells = ".regionCells";

    /// <summary>
    /// Enables displaying the choke points
    /// </summary>
    public const string ChokePoints = ".choke";

    /// <summary>
    /// Enables displaying the unexplored tiles
    /// </summary>
    public const string Exploration = ".explored";

    /// <summary>
    /// Enables displaying the name of every unit and effects
    /// </summary>
    public const string Names = ".names";

    /// <summary>
    /// Enables displaying the coordinates of each cell
    /// </summary>
    public const string Coordinates = ".coords";

    /// <summary>
    /// Enables displaying the defense scores of each region
    /// </summary>
    public const string Defense = ".def";
}
