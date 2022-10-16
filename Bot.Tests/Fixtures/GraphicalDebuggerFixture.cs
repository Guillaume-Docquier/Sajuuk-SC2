using Bot.Debugging.GraphicalDebugging;

namespace Bot.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class GraphicalDebuggerFixture {
    public GraphicalDebuggerFixture() {
        Program.GraphicalDebugger = new LadderGraphicalDebugger();
    }
}
