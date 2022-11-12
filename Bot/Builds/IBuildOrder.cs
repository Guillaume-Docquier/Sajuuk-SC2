using System.Collections.Generic;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds;

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

    // TODO GD Ideally we don't need this
    void AddRequest(BuildRequest buildRequest);

    /// <summary>
    /// Allows the build order to react to a new enemy strategy
    /// Certain builds will have to completely switch, others might shuffle BuildRequests
    /// </summary>
    /// <param name="enemyStrategy"></param>
    void ReactTo(EnemyStrategy enemyStrategy);
}
