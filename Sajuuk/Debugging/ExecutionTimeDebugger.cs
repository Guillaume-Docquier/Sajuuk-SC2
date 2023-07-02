using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sajuuk.ExtensionMethods;

namespace Sajuuk.Debugging;

public class ExecutionTimeDebugger {
    private readonly Dictionary<string, Stopwatch> _timers = new Dictionary<string, Stopwatch>();

    public void StartTimer(string tag) {
        if (_timers.ContainsKey(tag)) {
            return;
        }

        _timers[tag] = new Stopwatch();
        _timers[tag].Start();
    }

    public void StopTimer(string tag) {
        if (!_timers.ContainsKey(tag)) {
            return;
        }

        _timers[tag].Stop();
    }

    public double GetExecutionTime(string tag) {
        if (!_timers.ContainsKey(tag)) {
            return 0;
        }

        return _timers[tag].GetElapsedTimeMs();
    }

    public void LogExecutionTimes(string title) {
        var performances = _timers.Select(kv => $"{kv.Key} {kv.Value.GetElapsedTimeMs():F2}ms");

        Logger.Performance($"{title} " + string.Join(" | ", performances));
    }

    public void Reset() {
        foreach (var (_, timer) in _timers) {
            timer.Stop();
        }

        _timers.Clear();
    }
}
