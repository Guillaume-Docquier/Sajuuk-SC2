using System.Collections.Generic;
using System.Numerics;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public abstract class ScoutingTask {
    public Vector3 ScoutLocation { get; }

    public int Priority { get; }

    public int MaxScouts { get; }

    public ScoutingTask(Vector3 scoutLocation, int priority, int maxScouts) {
        ScoutLocation = scoutLocation;
        Priority = priority;
        MaxScouts = maxScouts;
    }

    public abstract bool IsComplete();

    public abstract void Cancel();

    public abstract void Execute(HashSet<Unit> scouts);
}
