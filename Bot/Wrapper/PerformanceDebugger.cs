using System.Diagnostics;
using Bot.ExtensionMethods;

namespace Bot.Wrapper;

public class PerformanceDebugger {
    private double _dataPointsCount = 0;

    private double _frameTotalTime = 0;
    private double _botTotalTime = 0;
    private double _controllerTotalTime = 0;
    private double _actionsTotalTime = 0;
    private double _debuggerTotalTime = 0;

    public readonly Stopwatch FrameStopwatch = new Stopwatch();
    public readonly Stopwatch BotStopwatch = new Stopwatch();
    public readonly Stopwatch ControllerStopwatch = new Stopwatch();
    public readonly Stopwatch ActionsStopwatch = new Stopwatch();
    public readonly Stopwatch DebuggerStopwatch = new Stopwatch();

    public void LogTimers(int actionCount) {
        var frameTime = FrameStopwatch.GetElapsedTimeMs();
        var controllerTime = ControllerStopwatch.GetElapsedTimeMs();
        var botTime = BotStopwatch.GetElapsedTimeMs();
        var actionsTime = ActionsStopwatch.GetElapsedTimeMs();
        var debuggerTime = DebuggerStopwatch.GetElapsedTimeMs();

        Logger.Debug(
            "Actions {0,3} | Frame {1,5:F2} ms | Controller ({2,3:P0}) {3,5:F2} ms | Bot ({4,3:P0}) {5,5:F2} ms | Actions ({6,3:P0}) {7,5:F2} ms | Debugger ({8,3:P0}) {9,5:F2} ms",
            actionCount,
            frameTime,
            controllerTime / frameTime,
            controllerTime,
            botTime / frameTime,
            botTime,
            actionsTime / frameTime,
            actionsTime,
            debuggerTime / frameTime,
            debuggerTime
        );
    }

    public void LogAveragePerformance() {
        var averageControllerPercent = _controllerTotalTime / _frameTotalTime;
        var averageBotPercent = _botTotalTime / _frameTotalTime;
        var averageActionsPercent = _actionsTotalTime / _frameTotalTime;
        var averageDebuggerPercent = _debuggerTotalTime / _frameTotalTime;

        var averageFrameTime = _frameTotalTime / _dataPointsCount;
        var averageControllerTime = _controllerTotalTime / _dataPointsCount;
        var averageBotTime = _botTotalTime / _dataPointsCount;
        var averageActionsTime = _actionsTotalTime / _dataPointsCount;
        var averageDebuggerTime = _debuggerTotalTime / _dataPointsCount;

        Logger.Debug(
            "Average performance: Frame {0,5:F2} ms | Controller ({1,3:P0}) {2,5:F2} ms | Bot ({3,3:P0}) {4,5:F2} ms | Actions ({5,3:P0}) {6,5:F2} ms | Debugger ({7,3:P0}) {8,5:F2} ms",
            averageFrameTime,
            averageControllerPercent,
            averageControllerTime,
            averageBotPercent,
            averageBotTime,
            averageActionsPercent,
            averageActionsTime,
            averageDebuggerPercent,
            averageDebuggerTime
        );
    }

    public void CompileData() {
        _dataPointsCount++;

        _frameTotalTime += FrameStopwatch.GetElapsedTimeMs();
        _controllerTotalTime += ControllerStopwatch.GetElapsedTimeMs();
        _botTotalTime += BotStopwatch.GetElapsedTimeMs();
        _actionsTotalTime += ActionsStopwatch.GetElapsedTimeMs();
        _debuggerTotalTime += DebuggerStopwatch.GetElapsedTimeMs();

        ResetTimers();
    }

    public void ResetTimers() {

        FrameStopwatch.Reset();
        ControllerStopwatch.Reset();
        BotStopwatch.Reset();
        ActionsStopwatch.Reset();
        DebuggerStopwatch.Reset();
    }
}
