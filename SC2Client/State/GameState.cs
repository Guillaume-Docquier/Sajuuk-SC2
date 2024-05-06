using System.Numerics;
using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client.GameData;

namespace SC2Client.State;

public class GameState : IGameState {
    private readonly Terrain _terrain;
    private readonly Units _units;

    public uint PlayerId { get; }
    public uint CurrentFrame { get; private set; }
    public Result Result { get; private set; }
    public Vector2 StartingLocation { get; }
    public Vector2 EnemyStartingLocation { get; }
    public ITerrain Terrain => _terrain;
    public IUnits Units => _units;

    public GameState(
        uint playerId,
        ILogger logger,
        KnowledgeBase knowledgeBase,
        ResponseGameInfo gameInfo,
        Status gameStatus,
        ResponseObservation observation
    ) {
        PlayerId = playerId;
        CurrentFrame = observation.Observation.GameLoop;
        Result = GetGameResult(gameStatus, observation);
        _terrain = new Terrain(gameInfo);
        _units = new Units(logger, knowledgeBase, observation);

        var startingTownHallPosition = UnitQueries.GetUnits(_units.OwnedUnits, UnitTypeId.TownHalls).First().Position.ToVector2();
        var startLocations = gameInfo.StartRaw.StartLocations
            .Select(startLocation => new Vector2(startLocation.X, startLocation.Y))
            .ToList();

        StartingLocation = startLocations.MinBy(startLocation => startLocation.DistanceTo(startingTownHallPosition));
        EnemyStartingLocation = startLocations.MaxBy(startLocation => startLocation.DistanceTo(startingTownHallPosition));
    }

    /// <summary>
    /// Updates the game state.
    /// </summary>
    /// <param name="gameStatus">The status of the game.</param>
    /// <param name="observation">The current game state observation.</param>
    public void Update(Status gameStatus, ResponseObservation observation) {
        CurrentFrame = observation.Observation.GameLoop;
        Result = GetGameResult(gameStatus, observation);

        // The order of updates is not important
        _terrain.Update(observation);
        _units.Update(observation);
    }

    /// <summary>
    /// Gets the game result from the response.
    /// </summary>
    /// <param name="gameStatus">The status of the game.</param>
    /// <param name="observation">The current game state observation.</param>
    /// <returns>The current game result.</returns>
    private Result GetGameResult(Status gameStatus, ResponseObservation observation) {
        switch (gameStatus) {
            case Status.Quit:
                return Result.Defeat;
            case Status.Ended:
                return observation.PlayerResult.First(result => result.PlayerId == PlayerId).Result;
            default:
                return Result.Undecided;
        }
    }
}
