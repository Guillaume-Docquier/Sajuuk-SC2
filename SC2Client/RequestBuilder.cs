using SC2APIProtocol;

namespace SC2Client;

public static class RequestBuilder {
    /**
     * Creates an SC2 API request to create a computer game.
     */
    public static Request RequestCreateComputerGame(bool realTime, string mapPath, Race opponentRace, Difficulty opponentDifficulty) {
        var requestCreateGame = new RequestCreateGame
        {
            Realtime = realTime,
            LocalMap = new LocalMap
            {
                MapPath = mapPath,
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

    /**
     * Creates an SC2 API request to join an AIArena ladder game.
     */
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

    /**
     * Creates an SC2 API request to join a local game.
     */
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

    /**
     * Creates game configuration options that determines what data the game sends when sending game state on each frame.
     */
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

    /**
     * Creates an SC2 API request to quit a game.
     */
    public static Request RequestQuitGame() {
        return new Request
        {
            Quit = new RequestQuit()
        };
    }
}
