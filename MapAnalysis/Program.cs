using MapAnalysis;
using SC2APIProtocol;
using SC2Client;

var mapsToAnalyze = Maps.GetAll()
    // Blackburn has an isolated expand that breaks the analysis, we'll fix it if it comes back to the map pool
    .Except(new[] { Maps.Blackburn })
    .ToList();

var mapIndex = 1;
foreach (var mapToAnalyze in mapsToAnalyze) {
    var services = ServicesFactory.CreateServices(mapToAnalyze);
    var logger = services.Logger.CreateNamed("Game Loop");

    logger.Important($"Analyzing map: {mapToAnalyze} ({mapIndex}/{mapsToAnalyze.Count})");

    var game = await services.GameConnection.JoinGame(Race.Zerg);
    while (!services.MapAnalyzer.IsAnalysisComplete) {
        foreach (var tracker in services.Trackers) {
            tracker.Update(game.State);
        }

        services.MapAnalyzer.OnFrame(game.State);

        // TODO GD This doesn't seem proper
        var debugRequest = services.GraphicalDebugger.GetDebugRequest();
        if (debugRequest != null) {
            await services.Sc2Client.SendRequest(debugRequest);
        }

        await game.Step(stepSize: 1, new List<SC2APIProtocol.Action>());
    }

    logger.Success($"Analysis on {mapToAnalyze} complete!");

    game.Quit();

    mapIndex++;
}
