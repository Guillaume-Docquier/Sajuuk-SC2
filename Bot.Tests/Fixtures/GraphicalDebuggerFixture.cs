using Bot.Debugging.GraphicalDebugging;

namespace Bot.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class GraphicalDebuggerFixture {
    public GraphicalDebuggerFixture() {
        Program.GraphicalDebugger = new NullGraphicalDebugger();
    }
}
