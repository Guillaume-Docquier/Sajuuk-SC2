using Jakuul;
using SC2APIProtocol;
using SC2Client;
using SC2Client.Trackers;

// CLI
var commandLineArgs = new CommandLineArguments(args);

// DI
var frameClock = new FrameClock();
var logger = new Logger(frameClock, logToStdOut: true);
var sc2Client = new Sc2Client(logger, GameDisplayMode.FullScreen);

IGameConnection sc2GameConnection = commandLineArgs.LadderServerAddress != null
    ? new LadderGameConnection(logger, sc2Client, commandLineArgs.LadderServerAddress, commandLineArgs.GamePort, commandLineArgs.StartPort)
    : new LocalGameConnection(logger, sc2Client, new LocalGameConfiguration());

// Play
var game = await sc2GameConnection.JoinGame(Race.Zerg); // TODO GD The bot's race will be defined by the bot
while (game.State.Result == Result.Undecided) {
    // bot.Play(game.State)
    await game.Step(stepSize: 2, new List<SC2APIProtocol.Action>()); // TODO GD Provide the actions
}

logger.Success($"Game has ended: {game.State.Result}");
