using System.Collections.Generic;
using System.Linq;

namespace Sajuuk.Debugging;

public static class DebuggingCommands {
    public static HashSet<string> GetAll() {
        return typeof(DebuggingCommands).GetFields().Select(x => x.GetValue(null)).Cast<string>().ToHashSet();
    }

    /// <summary>
    /// Resets the debugging flags to their default
    /// </summary>
    public const string Reset = ".reset";

    /// <summary>
    /// Turn off all the debugging flags
    /// </summary>
    public const string Off = ".off";
}
