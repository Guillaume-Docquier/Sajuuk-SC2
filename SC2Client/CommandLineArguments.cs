namespace SC2Client;

/// <summary>
/// This CLI parser parses the CLI arguments given by AIArena.
/// </summary>
public class CommandLineArguments {
    public CommandLineArguments(IReadOnlyList<string> args) {
        for (var i = 0; i < args.Count; i += 2) {
            var flag = args[i];
            var value = args[i + 1];

            switch (flag) {
                case "-g":
                case "--GamePort":
                    GamePort = int.Parse(value);
                    break;
                case "-o":
                case "--StartPort":
                    StartPort = int.Parse(value);
                    break;
                case "-l":
                case "--LadderServer":
                    LadderServerAddress = value;
                    break;
            }
        }
    }

    public int GamePort { get; }

    public int StartPort { get; }

    public string? LadderServerAddress { get; }
}
