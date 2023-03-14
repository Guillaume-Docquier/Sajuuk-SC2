using System.Linq;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense;

public class IncomeTracker : INeedUpdating {
    public static readonly IncomeTracker Instance = new IncomeTracker();

    private readonly CircularQueue<float> _mineralsCollectionRates = new CircularQueue<float>((int)(TimeUtils.FramesPerSecond * 30));
    private readonly CircularQueue<float> _vespeneCollectionRates = new CircularQueue<float>((int)(TimeUtils.FramesPerSecond * 30));

    public static float CurrentMineralsCollectionRate { get; private set; }
    public static float MaxMineralsCollectionRate { get; private set; }
    public static float AverageMineralsCollectionRate { get; private set; }

    public static float CurrentVespeneCollectionRate { get; private set; }
    public static float MaxVespeneCollectionRate { get; private set; }
    public static float AverageVespeneCollectionRate { get; private set; }

    private IncomeTracker() {}

    public void Reset() {
        _mineralsCollectionRates.Clear();
        _vespeneCollectionRates.Clear();
    }

    public void Update(ResponseObservation observation) {
        var scoreDetails = observation.Observation.Score.ScoreDetails;
        UpdateMineralsCollectionRates(scoreDetails.CollectionRateMinerals);
        UpdateVespeneCollectionRates(scoreDetails.CollectionRateVespene);
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
