using System.Numerics;

namespace Sajuuk;

public class OrderAction
{
    public Vector2? TargetPosition { get;set; }
    public ulong? TargetUnit { get; set; }
    public uint AbilityId { get; set; }
    public bool QueueComannd { get; set; }
}
