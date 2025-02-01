using System.Numerics;

namespace Algorithms;

// TODO GD Remove this and use a getDistance/getPosition functor instead?
public interface IHavePosition {
    public Vector3 Position { get; }
}
