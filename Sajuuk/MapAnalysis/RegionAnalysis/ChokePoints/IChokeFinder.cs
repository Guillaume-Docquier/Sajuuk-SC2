using System.Collections.Generic;

namespace Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

public interface IChokeFinder {
    List<ChokePoint> FindChokePoints();
}
