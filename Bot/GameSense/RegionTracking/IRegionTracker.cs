using System.Numerics;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

public interface IRegionTracker {
    public float GetForce(Vector2 position, Alliance alliance, bool normalized = false);
    public float GetForce(IRegion region, Alliance alliance, bool normalized = false);

    public float GetValue(Vector2 position, Alliance alliance, bool normalized = false);
    public float GetValue(IRegion region, Alliance alliance, bool normalized = false);

    public float GetThreat(Vector2 position, Alliance alliance, bool normalized = false);
    public float GetThreat(IRegion region, Alliance alliance, bool normalized = false);
}
