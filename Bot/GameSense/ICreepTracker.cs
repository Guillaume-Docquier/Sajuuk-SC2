using System.Collections.Generic;
using System.Numerics;

namespace Bot.GameSense;

public interface ICreepTracker {
    public bool HasCreep(Vector2 position);
    public List<Vector2> GetCreepFrontier();
}
