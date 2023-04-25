using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public interface IExpandAnalyzer {
    public bool IsInitialized { get; }
    public List<ExpandLocation> ExpandLocations { get; }

    public bool IsNotBlockingExpand(Vector2 position);
    public ExpandLocation GetExpand(Alliance alliance, ExpandType expandType);
}
