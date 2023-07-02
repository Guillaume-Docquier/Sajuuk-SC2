using System.Collections.Generic;
using System.Numerics;

namespace Sajuuk.GameSense;

public interface ICreepTracker {
    public bool HasCreep(Vector2 position);
    public List<Vector2> GetCreepFrontier();
}
