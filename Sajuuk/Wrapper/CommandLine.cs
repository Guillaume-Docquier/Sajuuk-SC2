﻿using System.Collections.Generic;
using SC2APIProtocol;

namespace Sajuuk.Wrapper;

internal class CommandLineArguments {
    public CommandLineArguments(IReadOnlyList<string> args) {
        for (var i = 0; i < args.Count; i += 2) {
            if (args[i] == "-g" || args[i] == "--GamePort") {
                GamePort = int.Parse(args[i + 1]);
            }
            else if (args[i] == "-o" || args[i] == "--StartPort") {
                StartPort = int.Parse(args[i + 1]);
            }
            else if (args[i] == "-l" || args[i] == "--LadderServer") {
                LadderServer = args[i + 1];
            }
            else if (args[i] == "-c" || args[i] == "--ComputerOpponent") {
                if (ComputerRace == Race.NoRace) ComputerRace = Race.Random;
                // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
                if (ComputerDifficulty == 0) ComputerDifficulty = Difficulty.VeryHard;
                i--;
            }
            else if (args[i] == "-a" || args[i] == "--ComputerRace") {
                if (args[i + 1] == "Protoss") ComputerRace = Race.Protoss;
                else if (args[i + 1] == "Terran") ComputerRace = Race.Terran;
                else if (args[i + 1] == "Zerg") ComputerRace = Race.Zerg;
                else if (args[i + 1] == "Random") ComputerRace = Race.Random;
            }
            else if (args[i] == "-d" || args[i] == "--ComputerDifficulty") {
                if (args[i + 1] == "VeryEasy") ComputerDifficulty = Difficulty.VeryEasy;
                else if (args[i + 1] == "Easy") ComputerDifficulty = Difficulty.Easy;
                else if (args[i + 1] == "Medium") ComputerDifficulty = Difficulty.Medium;
                else if (args[i + 1] == "MediumHard") ComputerDifficulty = Difficulty.MediumHard;
                else if (args[i + 1] == "Hard") ComputerDifficulty = Difficulty.Hard;
                else if (args[i + 1] == "Harder") ComputerDifficulty = Difficulty.Harder;
                else if (args[i + 1] == "VeryHard") ComputerDifficulty = Difficulty.VeryHard;
                else if (args[i + 1] == "CheatVision") ComputerDifficulty = Difficulty.CheatVision;
                else if (args[i + 1] == "CheatMoney") ComputerDifficulty = Difficulty.CheatMoney;
                else if (args[i + 1] == "CheatInsane") ComputerDifficulty = Difficulty.CheatInsane;
            }
        }
    }

    public int GamePort { get; set; }

    public int StartPort { get; set; }

    public string LadderServer { get; set; }

    public Race ComputerRace { get; set; } = Race.NoRace;

    public Difficulty ComputerDifficulty { get; set; } = 0;
}
