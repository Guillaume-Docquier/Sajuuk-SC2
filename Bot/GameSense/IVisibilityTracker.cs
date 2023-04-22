using System.Collections.Generic;
using System.Numerics;

namespace Bot.GameSense;

public interface IVisibilityTracker {
    public List<Vector2> VisibleCells { get; }
    public List<Vector2> ExploredCells { get; }

    public bool IsVisible(Unit unit);
    public bool IsVisible(Vector3 location);
    public bool IsVisible(Vector2 location);

    public bool IsExplored(Unit unit);
    public bool IsExplored(Vector3 location);
    public bool IsExplored(Vector2 location);
}
