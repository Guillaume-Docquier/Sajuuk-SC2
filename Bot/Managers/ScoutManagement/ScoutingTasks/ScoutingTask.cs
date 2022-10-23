using System.Collections.Generic;
using System.Numerics;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public abstract class ScoutingTask {
    public Vector3 ScoutLocation { get; }

    public ScoutingTask(Vector3 scoutLocation) {
        ScoutLocation = scoutLocation;
    }

    public abstract bool IsComplete();

    public abstract void Cancel();

    public abstract void Execute(HashSet<Unit> scouts);
}
