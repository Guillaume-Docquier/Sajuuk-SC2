using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bot.Builds;
using Bot.Builds.BuildOrders;
using Bot.Debugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.Managers.ScoutManagement;
using Bot.Scenarios;
using SC2APIProtocol;

namespace Bot;

public class SajuukBot: PoliteBot {
    private readonly List<Manager> _managers = new List<Manager>();

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

        _managers.ForEach(manager => manager.OnFrame());

        var managerRequests = GetManagersBuildRequests();
        AddressManagerRequests(managerRequests);

        _debugger.Debug(managerRequests
            .SelectMany(groupedBySupply => groupedBySupply.SelectMany(request => request))
            .ToList()
        );

        foreach (var unit in UnitsTracker.UnitsByTag.Values) {
            unit.ExecuteModules();
        }

        return Task.CompletedTask;
    }

    private void InitManagers() {
        var buildManager = new BuildManager(new TwoBasesRoach());
        _managers.Add(buildManager);

        _managers.Add(new SupplyManager(buildManager));
        _managers.Add(new ScoutManager());
        _managers.Add(new EconomyManager());
        _managers.Add(new WarManager());
        _managers.Add(new CreepManager());
        _managers.Add(new UpgradesManager());
    }

    private void AddressManagerRequests(List<List<List<BuildFulfillment>>> groupedManagersBuildRequests) {
        // TODO GD Add blocking steps, ensure expands
        foreach (var priorityGroups in groupedManagersBuildRequests) {
            foreach (var supplyGroups in priorityGroups) {
                if (supplyGroups[0].AtSupply > Controller.CurrentSupply) {
                    continue;
                }

                // TODO GD Interweave requests (i.e if we have 2 requests, 10 roaches and 10 drones, do 1 roach 1 drone, 1 roach 1 drone, etc)
                foreach (var buildStep in supplyGroups) {
                    while (buildStep.Remaining > 0) {
                        var buildStepResult = Controller.ExecuteBuildStep(buildStep);
                        if (buildStepResult == Controller.RequestResult.Ok) {
                            buildStep.Fulfill(1);
                            // TODO GD Look back if supply has unlocked other build orders
                        }
                        // Don't retry expands if they are all taken
                        else if (buildStep.BuildType == BuildType.Expand && buildStepResult == Controller.RequestResult.NoSuitableLocation) {
                            buildStep.Fulfill(1);
                        }
                        else {
                            // Impossible, stop trying
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the managers build requests grouped by priority and supply required
    /// </summary>
    /// <returns>Nested lists containing the managers build requests grouped by priority and supply required</returns>
    private List<List<List<BuildFulfillment>>> GetManagersBuildRequests() {
        var priorityGroups = _managers
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
}
