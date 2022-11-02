using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Builds;
using Bot.Debugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.Managers.ScoutManagement;
using Bot.Scenarios;
using SC2APIProtocol;

namespace Bot;

public class SajuukBot: PoliteBot {
    private readonly List<BuildRequest> _buildOrder = BuildOrders.TwoBasesRoach();
    private IEnumerable<BuildRequest> RemainingBuildOrder => _buildOrder
        .ToList() // Make a copy in case we edit _buildOrder
        .Where(buildRequest => buildRequest.Fulfillment.Remaining > 0);

    private readonly List<Manager> _managers = new List<Manager>();

    private const int PriorityChangePeriod = 100;
    private int _managerPriorityIndex = 0;

    public override string Name => "Sajuuk";

    public override Race Race => Race.Zerg;

    private readonly BotDebugger _debugger = new BotDebugger();

    public SajuukBot(string version, List<IScenario> scenarios = null) : base(version, scenarios) {}

    protected override Task DoOnFrame() {
        if (Controller.Frame == 0) {
            InitManagers();
        }
        else if (Controller.Frame == 2016) {
            Logger.Metric("Collected Minerals: {0}", Controller.Observation.Observation.Score.ScoreDetails.CollectedMinerals);
        }

        FollowBuildOrder();

        _managers.ForEach(manager => manager.OnFrame());

        // TODO GD Most likely we will consume all the larvae in one shot
        // TODO GD A larvae round robin might be more interesting?
        // Make sure everyone gets a turn
        if (Controller.Frame % PriorityChangePeriod == 0) {
            _managerPriorityIndex = (_managerPriorityIndex + 1) % _managers.Count;
        }

        AddressManagerRequests();
        FixSupply();

        _debugger.Debug(RemainingBuildOrder, GetManagersBuildRequests());

        foreach (var unit in UnitsTracker.UnitsByTag.Values) {
            unit.ExecuteModules();
        }

        return Task.CompletedTask;
    }

    private void InitManagers() {
        _managers.Add(new ScoutManager());
        _managers.Add(new EconomyManager());
        _managers.Add(new WarManager());
        _managers.Add(new CreepManager());
        _managers.Add(new UpgradesManager());
    }

    private void FollowBuildOrder() {
        foreach (var buildStep in RemainingBuildOrder) {
            while (buildStep.Fulfillment.Remaining > 0) {
                if (Controller.CurrentSupply < buildStep.AtSupply || Controller.ExecuteBuildStep(buildStep.Fulfillment) != Controller.RequestResult.Ok) {
                    return;
                }

                buildStep.Fulfillment.Fulfill(1);
            }

            if (buildStep.Fulfillment is QuantityFulfillment) {
                _buildOrder.Remove(buildStep);
            }
        }
    }

    private void AddressManagerRequests() {
        // TODO GD Check if we should stop when we can't fulfill a build order
        // Do everything you can
        foreach (var buildStep in GetManagersBuildRequests()) {
            if (buildStep.AtSupply > Controller.CurrentSupply) {
                continue;
            }

            while (!IsBuildOrderBlocking() && buildStep.Remaining > 0) {
                var buildStepResult = Controller.ExecuteBuildStep(buildStep);
                if (buildStepResult == Controller.RequestResult.Ok) {
                    buildStep.Fulfill(1);
                    FollowBuildOrder(); // Sometimes the build order will be unblocked
                }
                // Don't retry expands if they are all taken
                else if (buildStep.BuildType == BuildType.Expand && buildStepResult == Controller.RequestResult.NoSuitableLocation) {
                    buildStep.Fulfill(1);
                }
                else {
                    break;
                }
            }

            // Ensure expands get made
            if (buildStep.BuildType == BuildType.Expand && buildStep.Remaining > 0) {
                break;
            }
        }
    }

    private bool IsBuildOrderBlocking() {
        var nextBuildStep = RemainingBuildOrder.FirstOrDefault();

        return nextBuildStep != null
               &&nextBuildStep.AtSupply <= Controller.CurrentSupply
               && Controller.IsUnlocked(nextBuildStep.UnitOrUpgradeType);
    }

    private IEnumerable<BuildFulfillment> GetManagersBuildRequests() {
        var buildFulfillments = new List<BuildFulfillment>();
        for (var i = 0; i < _managers.Count; i++) {
            var managerIndex = (_managerPriorityIndex + i) % _managers.Count;

            buildFulfillments.AddRange(_managers[managerIndex].BuildFulfillments.Where(buildFulfillment => buildFulfillment.Remaining > 0));
        }

        // Prioritize expands
        return buildFulfillments.OrderBy(buildRequest => buildRequest.BuildType == BuildType.Expand ? 0 : 1);
    }

    // TODO GD Handle overlords dying early game
    private void FixSupply() {
        if (!RemainingBuildOrder.Any()
            && Controller.AvailableSupply <= 2
            && Controller.MaxSupply < KnowledgeBase.MaxSupplyAllowed
            && !Controller.GetProducersCarryingOrders(Units.Overlord).Any()) {
            _buildOrder.Add(new QuantityBuildRequest(BuildType.Train, Units.Overlord, quantity: 4));
        }
    }
}
