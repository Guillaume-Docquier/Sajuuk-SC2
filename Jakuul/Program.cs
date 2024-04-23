using Jakuul;
using SC2APIProtocol;
using SC2Client;

var commandLineArgs = new CommandLineArguments(args);

var frameClock = new FrameClock();
var logger = new Logger(frameClock, logToStdOut: true);
var sc2Client = new Sc2Client(logger, GameDisplayMode.FullScreen);

IGameConnection sc2GameConnection = commandLineArgs.LadderServerAddress != null
    ? new LadderGameConnection(logger, sc2Client, commandLineArgs.LadderServerAddress, commandLineArgs.GamePort, commandLineArgs.StartPort)
    : new LocalGameConnection(logger, sc2Client, new LocalGameConfiguration());

// TODO GD This will be defined by the bot
var botRace = Race.Zerg;

// TODO GD Return an IGame instead of player id
await sc2GameConnection.JoinGame(botRace);

// TODO GD Let the bot play
