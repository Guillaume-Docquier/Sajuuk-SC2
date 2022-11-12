using System.Collections.Generic;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds;

public interface IBuildOrder {
    // TODO GD Make this Readonly and provide methods to add / remove (i.e clear fulfilled quantity requests)
    List<BuildRequest> BuildRequests { get; }

    void ReactTo(EnemyStrategy enemyStrategy);
}
