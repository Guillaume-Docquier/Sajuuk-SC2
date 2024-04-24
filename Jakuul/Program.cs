using Jakuul;
using SC2APIProtocol;
using SC2Client;

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
var botRace = Race.Zerg; // TODO GD The bot's race will be defined by the bot
var game = await sc2GameConnection.JoinGame(botRace);

await game.Step(stepSize: 0);
while (game.GameResult == Result.Undecided) {
    // bot.Play(game)
    await game.Step(stepSize: 2);
}

logger.Success($"Game has ended: {game.GameResult}");
