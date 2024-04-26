using SC2APIProtocol;

namespace SC2Client;

/// <summary>
/// A collection of factory methods to create SC2 API requests.
/// The requests can be sent using an ISc2Client.
/// </summary>
public static class RequestBuilder {
    /// <summary>
    /// Creates an SC2 API request to create a computer game.
    /// </summary>
    /// <param name="realTime">Whether the game should be played in real time.</param>
    /// <param name="mapFilePath">The file path of the map to play on.</param>
    /// <param name="opponentRace">The race of the computer opponent.</param>
    /// <param name="opponentDifficulty">The difficulty of the computer opponent.</param>
    /// <returns>The request to create a computer game.</returns>
    public static Request RequestCreateComputerGame(bool realTime, string mapFilePath, Race opponentRace, Difficulty opponentDifficulty) {
        var requestCreateGame = new RequestCreateGame
        {
            Realtime = realTime,
            LocalMap = new LocalMap
            {
                MapPath = mapFilePath,
            }
        };

        requestCreateGame.PlayerSetup.Add(new PlayerSetup
        {
            Type = PlayerType.Participant,
        });

        requestCreateGame.PlayerSetup.Add(new PlayerSetup
        {
            Type = PlayerType.Computer,
            Race = opponentRace,
            Difficulty = opponentDifficulty,
        });

        return new Request
        {
            CreateGame = requestCreateGame,
        };
    }

    /// <summary>
    /// Creates an SC2 API request to join an AIArena ladder game.
    /// </summary>
    /// <param name="race">The race to play as.</param>
    /// <param name="startPort">The AIArena start port provided as CLI arguments.</param>
    /// <returns>The request to join a ladder game.</returns>
    public static Request RequestJoinLadderGame(Race race, int startPort) {
        var requestJoinGame = new RequestJoinGame
        {
            Race = race,
            SharedPort = startPort + 1,
            ServerPorts = new PortSet
            {
                GamePort = startPort + 2,
                BasePort = startPort + 3,
            },
            Options = GetInterfaceOptions(),
        };

        requestJoinGame.ClientPorts.Add(new PortSet
        {
            GamePort = startPort + 4,
            BasePort = startPort + 5
        });

        return new Request
        {
            JoinGame = requestJoinGame,
        };
    }

    /// <summary>
    /// Creates an SC2 API request to join a local game.
    /// </summary>
    /// <param name="race">The race to play as.</param>
    /// <returns>The request to join a local game.</returns>
    public static Request RequestJoinLocalGame(Race race) {
        return new Request
        {
            JoinGame = new RequestJoinGame
            {
                Race = race,
                Options = GetInterfaceOptions(),
            }
        };
    }

    /// <summary>
    /// Creates game interface options that determines what data the game sends when sending game state on each frame.
    /// </summary>
    /// <returns>The interface options.</returns>
    private static InterfaceOptions GetInterfaceOptions() {
        return new InterfaceOptions
        {
            Raw = true,
            Score = true,
            ShowCloaked = true,
            ShowBurrowedShadows = true,
            // ShowPlaceholders = true, // TODO GD Consider enabling this to simplify the BuildingsTracker?
        };
    }

    /// <summary>
    /// Creates an SC2 API request to quit a game.
    /// </summary>
    /// <returns>The request to quit the game.</returns>
    public static Request RequestQuitGame() {
        return new Request
        {
            Quit = new RequestQuit()
        };
    }

    /// <summary>
    /// Requests a new game observation.
    /// This will return the current game state.
    /// </summary>
    /// <param name="waitUntilFrame"></param>
    /// <returns></returns>
    public static Request RequestObservation(uint waitUntilFrame) {
        return new Request
        {
            Observation = new RequestObservation
            {
                GameLoop = waitUntilFrame,
            },
        };
    }

    /// <summary>
    /// Requests a step in the game simulation.
    /// </summary>
    /// <param name="stepSize"></param>
    /// <returns></returns>
    public static Request RequestStep(uint stepSize) {
        return new Request
        {
            Step = new RequestStep
            {
                Count = stepSize,
            }
        };
    }

    /// <summary>
    /// Requests drawing shapes in game.
    /// </summary>
    /// <param name="debugTexts">The texts to draw</param>
    /// <param name="debugSpheres">The spheres to draw</param>
    /// <param name="debugBoxes">The boxes to draw</param>
    /// <param name="debugLines">The lines to draw</param>
    /// <returns></returns>
    public static Request DebugDraw(
        IEnumerable<DebugText> debugTexts,
        IEnumerable<DebugSphere> debugSpheres,
        IEnumerable<DebugBox> debugBoxes,
        IEnumerable<DebugLine> debugLines
    ) {
        return new Request
        {
            Debug = new RequestDebug
            {
                Debug =
                {
                    new DebugCommand
                    {
                        Draw = new DebugDraw
                        {
                            Text = { debugTexts },
                            Spheres = { debugSpheres },
                            Boxes = { debugBoxes },
                            Lines = { debugLines },
                        },
                    },
                }
            }
        };
    }
}
