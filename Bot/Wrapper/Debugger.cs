using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public static class Debugger {
    public static async Task ShowDebugText(IReadOnlyCollection<string> texts) {
        var debugTextRequest = new Request
        {
            Debug = new RequestDebug
            {
                Debug =
                {
                    new DebugCommand
                    {
                        Draw = new DebugDraw
                        {
                            Text = { texts.Select(text => new DebugText { Text = text, Size = 18 }) },
                        }
                    }
                }
            }
        };

        await Program.GameConnection.SendRequest(debugTextRequest);
    }
}
