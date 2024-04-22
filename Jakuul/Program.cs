using Jakuul;
using SC2APIProtocol;
using SC2Client;

// TODO GD Get this from some env file
var mapFileName = Maps.CosmicSapphire;
var opponentRace = Race.Terran;
var opponentDifficulty = Difficulty.CheatInsane;
var realTime = true;

// TODO GD This will be defined by the bot
var botRace = Race.Zerg;

var frameClock = new FrameClock();
var logger = new Logger(frameClock, logToStdOut: true);
var sc2Client = new Sc2Client(logger, GameDisplayMode.FullScreen);
var sc2GameConnection = new LocalGameConnection(logger, sc2Client, mapFileName, opponentRace, opponentDifficulty, realTime); // TODO GD Parse command line arguments

// TODO GD Return an IGame instead of player id
await sc2GameConnection.JoinGame(botRace);

// TODO GD Let the bot play
