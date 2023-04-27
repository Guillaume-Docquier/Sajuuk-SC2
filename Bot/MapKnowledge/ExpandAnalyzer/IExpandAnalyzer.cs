using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public interface IExpandAnalyzer {
    public bool IsInitialized { get; }
    public IEnumerable<IExpandLocation> ExpandLocations { get; }

    public bool IsNotBlockingExpand(Vector2 position);
    public IExpandLocation GetExpand(Alliance alliance, ExpandType expandType);
}
