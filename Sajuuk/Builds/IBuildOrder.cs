using System.Collections.Generic;
using Sajuuk.GameSense.EnemyStrategyTracking;

namespace Sajuuk.Builds;

public interface IBuildOrder {
    /// <summary>
    /// Represents the current build requests for the build order
    /// </summary>
    IReadOnlyCollection<BuildRequest> BuildRequests { get; }

    /// <summary>
    /// Prunes the build order of any unwanted BuildRequests
    /// This will typically get rid of fulfilled QuantityBuildRequests
    /// </summary>
    void PruneRequests();

    /// <summary>
    /// Allows the build order to react to a new enemy strategy
    /// Certain builds will have to completely switch, others might shuffle BuildRequests
    /// </summary>
    /// <param name="newEnemyStrategy"></param>
    void ReactTo(EnemyStrategy newEnemyStrategy);
}
