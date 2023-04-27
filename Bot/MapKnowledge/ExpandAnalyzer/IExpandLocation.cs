using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapKnowledge;

public interface IExpandLocation {
    public Vector2 Position { get; }
    public ExpandType ExpandType { get; }
    public IReadOnlySet<Unit> Resources { get; }
    public bool IsDepleted { get; }
    public bool IsBlocked { get; } // TODO GD Not yet implemented, but sometimes there are resources or rocks blocking the expand
}
