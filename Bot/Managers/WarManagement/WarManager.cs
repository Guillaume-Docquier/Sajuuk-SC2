using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement;

/*
 * REFACTOR NOTES
 * Use a strategy to do each phase
 * - i.e EarlyGameRecruitmentPhaseStrategy
 *
 * Use a state machine to set the strategies, dispatchers, assigners and releasers
 *
 * Strategies themselves can use strategies or other states
 * - TerranFinisher
 * - CannonRush defense
 * - WorkerRush defense
 *
 * Be conservative about switching states try not to yo-yo
 */

public partial class WarManager: Manager {
    private readonly WarManagerBehaviour _behaviour;
    private readonly WarManagerDebugger _debugger = new WarManagerDebugger();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _behaviour.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    protected override IAssigner Assigner => _behaviour.Assigner;
    protected override IDispatcher Dispatcher => _behaviour.Dispatcher;
    protected override IReleaser Releaser => _behaviour.Releaser;

    public WarManager() {
        _behaviour = new WarManagerBehaviour(this);
    }

    public override string ToString() {
        return "WarManager";
    }

    protected override void StartOfFramePhase() {
        _behaviour.Update();
    }

    protected override void RecruitmentPhase() {
        _behaviour.RecruitmentPhaseStrategy.Execute();
    }

    protected override void DispatchPhase() {
        _behaviour.DispatchPhaseStrategy.Execute();
    }

    protected override void ManagementPhase() {
        _behaviour.ManagementPhaseStrategy.Execute();
        // TODO GD Debug managed units
    }
}
