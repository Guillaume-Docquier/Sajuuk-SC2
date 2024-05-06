using System.Numerics;
using Algorithms.ExtensionMethods;
using MapAnalysis.ExpandAnalysis;
using SC2Client;
using SC2Client.Debugging.GraphicalDebugging;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.Services;
using SC2Client.Trackers;

namespace MapAnalysis;

public static class ServicesFactory {
    public readonly struct Services {
        public ILogger Logger { get; init; }
        public IGameConnection GameConnection { get; init; }
        public ISc2Client Sc2Client { get; init; }
        public IGraphicalDebugger GraphicalDebugger { get; init; }
        public IAnalyzer MapAnalyzer { get; init; }
        public IReadOnlyList<ITracker> Trackers { get; init; }
    }

    public static Services CreateServices(string mapFileName) {
        var knowledgeBase = new KnowledgeBase();
        var frameClock = new FrameClock();
        var logger = new Logger(frameClock, logToStdOut: true);
        var sc2Client = new Sc2Client(logger, GameDisplayMode.FullScreen);
        var gameConnection = new LocalGameConnection(logger, sc2Client, knowledgeBase, new LocalGameConfiguration(mapFileName));

        var footprintCalculator = new FootprintCalculator(knowledgeBase, logger);
        var terrainTracker = new TerrainTracker();
        var graphicalDebugger = new Sc2GraphicalDebugger(terrainTracker);

        var pathfinder = new CachedPathfinder<Vector2>(
            logger,
            new PathfinderCache<Vector2>(),
            (from, to) => from.DistanceTo(to),
            cell => terrainTracker.GetReachableNeighbors(cell),
            cell => terrainTracker.GetClosestWalkable(cell).AsWorldGridCenter(),
            vector => vector.ToString()
        );

        var expandUnitsAnalyzer = new ExpandUnitsAnalyzer();
        var expandAnalyzer = new ExpandAnalyzer(
            knowledgeBase,
            frameClock,
            logger,
            sc2Client,
            graphicalDebugger,
            terrainTracker,
            expandUnitsAnalyzer,
            pathfinder,
            footprintCalculator
        );

        // TODO GD Make it simpler to know who's a tracker and in which order to update them
        var trackers = new List<ITracker> { frameClock, terrainTracker };

        var analyzers = new List<IAnalyzer> { expandAnalyzer };
        var mapAnalyzer = new MapAnalyzer(logger, analyzers);

        return new Services
        {
            Logger = logger,
            GameConnection = gameConnection,
            Sc2Client = sc2Client,
            GraphicalDebugger = graphicalDebugger,
            MapAnalyzer = mapAnalyzer,
            Trackers = trackers,
        };
    }
}
