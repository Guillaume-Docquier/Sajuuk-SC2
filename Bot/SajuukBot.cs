using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Builds;
using Bot.Builds.BuildOrders;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.Scenarios;
using Bot.Tagging;
using SC2APIProtocol;

namespace Bot;

public class SajuukBot : PoliteBot {
    private readonly List<Manager> _managers = new List<Manager>();

    private readonly IManagerFactory _managerFactory;
    private readonly IBuildRequestFactory _buildRequestFactory;
    private readonly IBuildOrderFactory _buildOrderFactory;

    private readonly IBotDebugger _debugger;

    public override string Name => "Sajuuk";
    public override Race Race => Race.Zerg;

    public SajuukBot(
        string version,
        List<IScenario> scenarios,
        ITaggingService taggingService,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IManagerFactory managerFactory,
        IBuildRequestFactory buildRequestFactory,
        IBuildOrderFactory buildOrderFactory,
        IBotDebugger botDebugger
    ) : base(version, scenarios, taggingService, unitsTracker, terrainTracker) {
        _managerFactory = managerFactory;
        _buildRequestFactory = buildRequestFactory;
        _buildOrderFactory = buildOrderFactory;

        _debugger = botDebugger;
    }

    protected override Task DoOnFrame() {
        if (Controller.Frame == 0) {
            InitManagers();
        }

        _managers.ForEach(manager => manager.OnFrame());

        var managerRequests = GetManagersBuildRequests();
        var buildBlockStatus = AddressManagerRequests(managerRequests);
        EnsureNoSupplyBlock();

        var flatManagerRequests = managerRequests
            .SelectMany(groupedBySupply => groupedBySupply.SelectMany(request => request))
            .ToList();

        SpendingTracker.Instance.UpdateExpectedFutureSpending(flatManagerRequests);

        _debugger.Debug(flatManagerRequests, buildBlockStatus);

        foreach (var unit in UnitsTracker.UnitsByTag.Values) {
            unit.ExecuteModules();
        }

        return Task.CompletedTask;
    }

    private void InitManagers() {
        var buildManager = _managerFactory.CreateBuildManager(_buildOrderFactory.CreateTwoBasesRoach());
        _managers.Add(buildManager);

        _managers.Add(_managerFactory.CreateSupplyManager(buildManager));
        _managers.Add(_managerFactory.CreateScoutManager());
        _managers.Add(_managerFactory.CreateEconomyManager(buildManager));
        _managers.Add(_managerFactory.CreateWarManager());
        _managers.Add(_managerFactory.CreateCreepManager());
        _managers.Add(_managerFactory.CreateUpgradesManager());
    }

    /// <summary>
    /// Try to fulfill the manager requests in a smart order.
    /// Orders are sorted by priority and supply timing.
    /// When we add an order that changes the supply, we look back to see if a previous order now meets its timing.
    /// </summary>
    /// <param name="groupedManagersBuildRequests"></param>
    /// <returns>The blocking build fulfillment with the block reason, or (null, None) if nothing was blocking</returns>
    private static (BuildFulfillment, BuildBlockCondition) AddressManagerRequests(List<List<List<BuildFulfillment>>> groupedManagersBuildRequests) {
        var lookBackSupplyTarget = long.MaxValue;
        foreach (var priorityGroups in groupedManagersBuildRequests) {
            foreach (var supplyGroups in priorityGroups) {
                if (supplyGroups[0].AtSupply > Controller.CurrentSupply) {
                    lookBackSupplyTarget = Math.Min(lookBackSupplyTarget, supplyGroups[0].AtSupply);
                    continue;
                }

                // TODO GD Interweave requests (i.e if we have 2 requests, 10 roaches and 10 drones, do 1 roach 1 drone, 1 roach 1 drone, etc)
                foreach (var buildStep in supplyGroups) {
                    while (buildStep.Remaining > 0) {
                        var buildStepResult = Controller.ExecuteBuildStep(buildStep);
                        if (buildStepResult == BuildRequestResult.Ok) {
                            buildStep.Fulfill(1);

                            if (Controller.CurrentSupply >= lookBackSupplyTarget) {
                                return AddressManagerRequests(groupedManagersBuildRequests);
                            }
                        }
                        // Don't retry expands if they are all taken
                        else if (buildStep.BuildType == BuildType.Expand && buildStepResult.HasFlag(BuildRequestResult.NoSuitableLocation)) {
                            buildStep.Fulfill(1);
                        }
                        else if (ShouldBlock(buildStep, buildStepResult, out var buildBlockingReason)) {
                            // We must wait to fulfill this one
                            // TODO GD We can still process other requests that don't overlap with the blocking condition
                            return (buildStep, buildBlockingReason);
                        }
                        else {
                            // Not possible anymore, go to next
                            break;
                        }
                    }
                }
            }
        }

        return (null, BuildBlockCondition.None);
    }

    /// <summary>
    /// Returns the managers build requests grouped by priority and supply required
    /// </summary>
    /// <returns>Nested lists containing the managers build requests grouped by priority and supply required</returns>
    private List<List<List<BuildFulfillment>>> GetManagersBuildRequests() {
        // We shuffle so that the requests are not always in the same order
        // We cannot shuffle request groups because we want to preserve the relative order of a manager's requests
        var shuffledManagers = _managers.ToList().Shuffle();

        var priorityGroups = shuffledManagers
            .SelectMany(manager => manager.BuildFulfillments.Where(buildFulfillment => buildFulfillment.Remaining > 0))
            .GroupBy(buildRequest => buildRequest.Priority)
            .OrderByDescending(group => group.Key);

        var buildFulfillments = priorityGroups
            .Select(priorityGroup => priorityGroup
                .GroupBy(buildRequest => buildRequest.AtSupply)
                // AtSupply 0 should be last because it means we don't care about the supply value
                .OrderBy(group => group.Key == 0 ? int.MaxValue : group.Key)
                .Select(supplyGroup => supplyGroup.ToList())
                .ToList()
            )
            .ToList();

        return buildFulfillments;
    }

    /// <summary>
    /// Determines if a BuildFulfillment should block the build based on its block conditions and the request result.
    /// </summary>
    /// <param name="buildFulfillment">The build fulfillment that might be blocking</param>
    /// <param name="buildRequestResult">The build request result</param>
    /// <param name="buildBlockingReason">The blocking reason, if any</param>
    /// <returns>True if the build should be blocked</returns>
    private static bool ShouldBlock(BuildFulfillment buildFulfillment, BuildRequestResult buildRequestResult, out BuildBlockCondition buildBlockingReason) {
        // TODO GD All of this is not really cute, is there a nicer way?
        if (buildRequestResult.HasFlag(BuildRequestResult.TechRequirementsNotMet) && buildFulfillment.BlockCondition.HasFlag(BuildBlockCondition.MissingTech)) {
            buildBlockingReason = BuildBlockCondition.MissingTech;
            return true;
        }

        if (buildRequestResult.HasFlag(BuildRequestResult.NoProducersAvailable) && buildFulfillment.BlockCondition.HasFlag(BuildBlockCondition.MissingProducer)) {
            buildBlockingReason = BuildBlockCondition.MissingProducer;
            return true;
        }

        if (buildRequestResult.HasFlag(BuildRequestResult.NotEnoughMinerals) && buildFulfillment.BlockCondition.HasFlag(BuildBlockCondition.MissingMinerals)) {
            buildBlockingReason = BuildBlockCondition.MissingMinerals;
            return true;
        }

        if (buildRequestResult.HasFlag(BuildRequestResult.NotEnoughVespeneGas) && buildFulfillment.BlockCondition.HasFlag(BuildBlockCondition.MissingVespene)) {
            buildBlockingReason = BuildBlockCondition.MissingVespene;
            return true;
        }

        buildBlockingReason = BuildBlockCondition.None;
        return false;
    }

    private void EnsureNoSupplyBlock() {
        if (!Controller.IsSupplyBlocked) {
            return;
        }

        if (Controller.GetProducersCarryingOrders(Units.Overlord).Any()) {
            return;
        }

        if (Controller.GetProducersCarryingOrders(Units.Hatchery).Any()) {
            return;
        }

        Controller.ExecuteBuildStep(_buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Overlord).Fulfillment);
    }
}
