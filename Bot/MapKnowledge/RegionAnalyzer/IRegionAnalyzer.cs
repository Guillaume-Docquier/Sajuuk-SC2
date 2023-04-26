using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public interface IRegionAnalyzer {
    public bool IsInitialized { get; }
    public List<IRegion> Regions { get; }

    public Region GetNaturalExitRegion(Alliance alliance);
    public Region GetRegion(Vector2 position);
    public Region GetRegion(Vector3 position);
}
