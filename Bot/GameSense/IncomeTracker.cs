using System.Linq;
using Bot.DataStructures;
using Bot.GameData;
using Bot.Tagging;
using Bot.UnitModules;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense;

public class IncomeTracker : IIncomeTracker, INeedUpdating {
    /// <summary>
    /// DI: ✔️ The only usages are for static instance creations
    /// </summary>
    public static readonly IncomeTracker Instance = new IncomeTracker(TaggingService.Instance, UnitsTracker.Instance);

    private readonly ITaggingService _taggingService;
    private readonly IUnitsTracker _unitsTracker;

    private const int LogCollectedMineralsFrame = (int)(90 * TimeUtils.FramesPerSecond);
    private const int StatisticsRollingWindowSeconds = 30;

    private readonly CircularQueue<float> _mineralsCollectionRates = new CircularQueue<float>((int)(TimeUtils.FramesPerSecond * StatisticsRollingWindowSeconds));
    private readonly CircularQueue<float> _vespeneCollectionRates = new CircularQueue<float>((int)(TimeUtils.FramesPerSecond * StatisticsRollingWindowSeconds));

    public float CurrentMineralsCollectionRate { get; private set; }
    public float MaxMineralsCollectionRate { get; private set; }
    public float AverageMineralsCollectionRate { get; private set; }
    public float ExpectedMineralsCollectionRate { get; private set; }

    public float CurrentVespeneCollectionRate { get; private set; }
    public float MaxVespeneCollectionRate { get; private set; }
    public float AverageVespeneCollectionRate { get; private set; }
    public float ExpectedVespeneCollectionRate { get; private set; }

    private IncomeTracker(ITaggingService taggingService, IUnitsTracker unitsTracker) {
        _taggingService = taggingService;
        _unitsTracker = unitsTracker;
    }

    public void Reset() {
        _mineralsCollectionRates.Clear();
        _vespeneCollectionRates.Clear();
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        var scoreDetails = observation.Observation.Score.ScoreDetails;
        UpdateMineralsCollectionRates(scoreDetails.CollectionRateMinerals);
        UpdateVespeneCollectionRates(scoreDetails.CollectionRateVespene);

        CalculateExpectedCollectionRates();

        if (!_taggingService.HasTagged(Tag.Minerals) && Controller.Frame >= LogCollectedMineralsFrame) {
            var mineralsCollected = observation.Observation.Score.ScoreDetails.CollectedMinerals;
            Logger.Metric("Collected Minerals: {0}", mineralsCollected);
            _taggingService.TagMinerals(mineralsCollected);
        }
    }

    private void CalculateExpectedCollectionRates() {
        ExpectedMineralsCollectionRate = 0;
        ExpectedVespeneCollectionRate = 0;

        ExpectedMineralsCollectionRate += (float)Controller.GetUnits(_unitsTracker.NeutralUnits, Units.BlueMineralFields)
            .Select(UnitModule.Get<CapacityModule>)
            .Where(module => module != null)
            .Sum(module => ComputeResourceNodeExpectedCollectionRate(Resources.ResourceType.Mineral, module.AssignedUnits.Count));

        ExpectedMineralsCollectionRate += (float)Controller.GetUnits(_unitsTracker.NeutralUnits, Units.GoldMineralFields)
            .Select(UnitModule.Get<CapacityModule>)
            .Where(module => module != null)
            .Sum(module => ComputeResourceNodeExpectedCollectionRate(Resources.ResourceType.Mineral, module.AssignedUnits.Count, isGold: true));

        ExpectedVespeneCollectionRate += (float)Controller.GetUnits(_unitsTracker.NeutralUnits, Units.GreenGasGeysers)
            .Select(UnitModule.Get<CapacityModule>)
            .Select(module => module?.AssignedUnits?.FirstOrDefault())
            .Where(extractor => extractor != null)
            .Select(UnitModule.Get<CapacityModule>)
            .Sum(module => ComputeResourceNodeExpectedCollectionRate(Resources.ResourceType.Gas, module.AssignedUnits.Count));

        ExpectedVespeneCollectionRate += (float)Controller.GetUnits(_unitsTracker.NeutralUnits, Units.PurpleGasGeysers)
            .Select(UnitModule.Get<CapacityModule>)
            .Select(module => module?.AssignedUnits?.FirstOrDefault())
            .Where(extractor => extractor != null)
            .Select(UnitModule.Get<CapacityModule>)
            .Sum(module => ComputeResourceNodeExpectedCollectionRate(Resources.ResourceType.Gas, module.AssignedUnits.Count, isGold: true));
    }

    /// <summary>
    /// <para>
    /// Computes the expected collection rates based on current drone assignations and empirical data.
    /// </para>
    ///
    /// <para>
    /// Blue minerals<br/>
    /// - Empirically, we achieve a max of 122 per patch with 2 drones per patch and speed mining<br/>
    /// - Empirically, we achieve a max of 146 per patch with 3 drones per patch
    /// </para>
    ///
    /// <para>
    /// Gold minerals<br/>
    /// - Workers get 7 minerals per trip instead of 5<br/>
    /// - This is 40% more income
    /// </para>
    ///
    /// <para>
    /// Green gas<br/>
    /// - Empirically, we achieve an average of 162 per gas with 3 drones per gas
    /// </para>
    ///
    /// <para>
    /// Purple gas<br/>
    /// - Workers get 8 gas per trip instead of 4<br/>
    /// - This is 100% more income
    /// </para>
    /// </summary>
    /// <param name="resourceType">The resource type of the resource</param>
    /// <param name="workerCount">Amount of workers on that resource</param>
    /// <param name="isGold">Whether the resource is a gold node (more yield)</param>
    /// <returns>The expected collection rate given the parameters</returns>
    public static double ComputeResourceNodeExpectedCollectionRate(Resources.ResourceType resourceType, int workerCount, bool isGold = false) {
        const double goldMineralsMultiplier = 1.4;
        const int goldGasMultiplier = 2;

        return (resourceType, workerCount) switch
        {
            (Resources.ResourceType.Mineral,   1) => 61  * (isGold ? goldMineralsMultiplier : 1),
            (Resources.ResourceType.Mineral,   2) => 122 * (isGold ? goldMineralsMultiplier : 1),
            (Resources.ResourceType.Mineral, >=3) => 146 * (isGold ? goldMineralsMultiplier : 1),
            (Resources.ResourceType.Gas,       1) => 54  * (isGold ? goldGasMultiplier : 1),
            (Resources.ResourceType.Gas,       2) => 108 * (isGold ? goldGasMultiplier : 1),
            (Resources.ResourceType.Gas,     >=3) => 162 * (isGold ? goldGasMultiplier : 1),
            _ => 0
        };
    }

    private void UpdateMineralsCollectionRates(float currentCollectionRateMinerals) {
        CurrentMineralsCollectionRate = currentCollectionRateMinerals;
        _mineralsCollectionRates.Enqueue(currentCollectionRateMinerals);

        MaxMineralsCollectionRate = _mineralsCollectionRates.Max();
        AverageMineralsCollectionRate = _mineralsCollectionRates.Average();
    }

    private void UpdateVespeneCollectionRates(float currentCollectionRateVespene) {
        CurrentVespeneCollectionRate = currentCollectionRateVespene;
        _vespeneCollectionRates.Enqueue(currentCollectionRateVespene);

        MaxVespeneCollectionRate = _vespeneCollectionRates.Max();
        AverageVespeneCollectionRate = _vespeneCollectionRates.Average();
    }
}
