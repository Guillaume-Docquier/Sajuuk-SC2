﻿using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameSense.EnemyStrategyTracking;
using Bot.Tagging;

namespace Bot.Managers;

public class BuildManager : UnitlessManager, ISubscriber<EnemyStrategyTransition> {
    private readonly IBuildOrder _buildOrder;
    private readonly ITaggingService _taggingService;
    private readonly IController _controller;

    public bool IsBuildOrderDone { get; private set; } = false;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildOrder.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public BuildManager(
        ITaggingService taggingService,
        IEnemyStrategyTracker enemyStrategyTracker,
        IController controller,
        IBuildOrder buildOrder
    ) {
        _taggingService = taggingService;
        _controller = controller;

        _buildOrder = buildOrder;

        enemyStrategyTracker.Register(this);
    }

    protected override void ManagementPhase() {
        _buildOrder.PruneRequests();

        if (!IsBuildOrderDone && _buildOrder.BuildRequests.All(request => request.Fulfillment.Remaining <= 0)) {
            var scoreDetails = _controller.Observation.Observation.Score.ScoreDetails;
            _taggingService.TagBuildDone(_controller.CurrentSupply, scoreDetails.CollectedMinerals, scoreDetails.CollectedVespene);
            IsBuildOrderDone = true;
        }
    }

    public void Notify(EnemyStrategyTransition data) {
        _buildOrder.ReactTo(data.CurrentStrategy);
    }
}
