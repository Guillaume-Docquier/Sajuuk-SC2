﻿using System.Numerics;
using SC2Client.State;

namespace MapAnalysis.ExpandAnalysis;

public interface IExpandLocation {
    public Vector2 Position { get; }
    public ExpandType ExpandType { get; }
    public HashSet<IUnit> Resources { get; } // TODO GD We might not need to expose that
    public bool IsDepleted { get; }
    public bool IsBlocked { get; } // TODO GD Not yet implemented, but sometimes there are resources or rocks blocking the expand
    public IRegion Region { get; }
}
