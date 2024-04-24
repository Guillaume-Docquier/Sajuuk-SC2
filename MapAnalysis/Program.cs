using MapAnalysis;
using SC2APIProtocol;
using SC2Client;

var mapsToAnalyze = Maps.GetAll()
    // Blackburn has an isolated expand that breaks the analysis, we'll fix it if it comes back to the map pool
    .Except(new[] { Maps.Blackburn })
    .ToList();

var mapIndex = 1;
foreach (var mapToAnalyze in mapsToAnalyze) {
    // DI
    var frameClock = new FrameClock();
    var logger = new Logger(frameClock, logToStdOut: true);
    var sc2Client = new Sc2Client(logger, GameDisplayMode.FullScreen);

    // TODO GD Create the analyzers
    var mapAnalyzer = new MapAnalyzer(logger, new List<IAnalyzer>());

    var sc2GameConnection = new LocalGameConnection(logger, sc2Client, new LocalGameConfiguration(mapToAnalyze));

    // Analyze
    logger.Important($"Analyzing map: {mapToAnalyze} ({mapIndex}/{mapsToAnalyze.Count})");

    var game = await sc2GameConnection.JoinGame(Race.Zerg);

    await game.Step(stepSize: 0);
    while (!mapAnalyzer.IsAnalysisComplete) {
        frameClock.CurrentFrame = game.CurrentFrame;

        mapAnalyzer.OnFrame(game);
        await game.Step(stepSize: 1);
    }

    logger.Success($"Analysis on {mapToAnalyze} complete!");

    await sc2Client.LeaveCurrentGame();

    mapIndex++;
}
