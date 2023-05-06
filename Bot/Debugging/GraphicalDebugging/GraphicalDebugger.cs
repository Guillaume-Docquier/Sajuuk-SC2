namespace Bot.Debugging.GraphicalDebugging;

public static class GraphicalDebugger {
    public static IGraphicalDebugger Instance;

    public static IGraphicalDebugger GetInstance() {
        return Instance;
    }
}
