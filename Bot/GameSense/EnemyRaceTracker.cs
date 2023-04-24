using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.Tagging;
using SC2APIProtocol;

namespace Bot.GameSense;

public class EnemyRaceTracker : IEnemyRaceTracker, INeedUpdating {
    /// <summary>
    /// DI: ✔️ The only usages are for static instance creations
    /// </summary>
    public static readonly EnemyRaceTracker Instance = new EnemyRaceTracker(TaggingService.Instance, UnitsTracker.Instance);

    private readonly ITaggingService _taggingService;
    private readonly IUnitsTracker _unitsTracker;

    public Race EnemyRace { get; private set; } = Race.NoRace;

    public EnemyRaceTracker(ITaggingService taggingService, IUnitsTracker unitsTracker) {
        _taggingService = taggingService;
        _unitsTracker = unitsTracker;
    }

    public void Reset() {
        EnemyRace = Race.NoRace;
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (EnemyRace == Race.NoRace) {
            EnemyRace = GetStartingRace(gameInfo.PlayerInfo, observation.Observation.PlayerCommon.PlayerId);
            _taggingService.TagEnemyRace(EnemyRace);
        }

        if (EnemyRace == Race.Random) {
            var realRace = GetRealRace(_unitsTracker.EnemyUnits);
            if (realRace != Race.NoRace) {
                EnemyRace = realRace;
                _taggingService.TagEnemyRace(EnemyRace);
            }
        }
    }

    /// <summary>
    /// Gets the race of the enemy player as reported by the game data.
    /// The race can be one of: Terran, Protoss, Zerg or Random.
    /// </summary>
    /// <param name="playerInfos">The info about the players in the game.</param>
    /// <param name="selfPlayerId">Our player id.</param>
    /// <returns>The race of the enemy player as reported by the game data.</returns>
    private static Race GetStartingRace(IEnumerable<PlayerInfo> playerInfos, uint selfPlayerId) {
        return playerInfos
            .Where(playerInfo => playerInfo.Type != PlayerType.Observer)
            .First(playerInfo => playerInfo.PlayerId != selfPlayerId)
            .RaceRequested;
    }

    /// <summary>
    /// Get the real race of a player based on its units.
    /// </summary>
    /// <param name="units">The units of a player.</param>
    /// <returns>The race of the player based on their units, or Race.NoRace</returns>
    private static Race GetRealRace(IReadOnlyCollection<Unit> units) {
        if (units.Any(worker => Units.AllTerranUnits.Contains(worker.UnitType))) {
            return Race.Terran;
        }

        if (units.Any(worker => Units.AllProtossUnits.Contains(worker.UnitType))) {
            return Race.Protoss;
        }

        if (units.Any(worker => Units.AllZergUnits.Contains(worker.UnitType))) {
            return Race.Zerg;
        }

        return Race.NoRace;
    }
}
