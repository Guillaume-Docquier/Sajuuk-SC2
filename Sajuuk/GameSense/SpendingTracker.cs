using System;
using System.Collections.Generic;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameData;

namespace Sajuuk.GameSense;

// This doesn't need to be a global singleton-like class
// It could be created by Sajuuk and given to the EconomyManager
// But at the same time, we don't want two of these
// Soooo... yeah, whatever!
public class SpendingTracker : ISpendingTracker {
    private readonly IIncomeTracker _incomeTracker;
    private readonly KnowledgeBase _knowledgeBase;

    public float ExpectedFutureMineralsSpending { get; private set; }
    public float ExpectedFutureVespeneSpending { get; private set; }

    public SpendingTracker(
        IIncomeTracker incomeTracker,
        KnowledgeBase knowledgeBase
    ) {
        _incomeTracker = incomeTracker;
        _knowledgeBase = knowledgeBase;
    }

    /// <summary>
    /// Update the expected future spending.
    /// We look at the future build requests and we compile how much minerals and gas we want to spend.
    ///
    /// We limit the sum to the minerals and gas income of the next minute.
    /// This effectively prioritizes expenses that will happen soon.
    /// </summary>
    /// <param name="futureBuildRequests"></param>
    public void UpdateExpectedFutureSpending(List<IFulfillableBuildRequest> futureBuildRequests) {
        ExpectedFutureMineralsSpending = 0;
        ExpectedFutureVespeneSpending = 0;

        // TODO GD The list is sorted by priority, then supply, with "atSupply: 0" being last
        // Because of this, if there's a lot of things planned for the future, we won't be counting "atSupply: 0" build requests, but in reality we'll execute them before
        // We could (should) adjust the logic accordingly
        var spendingLimit = _incomeTracker.ExpectedMineralsCollectionRate + _incomeTracker.ExpectedVespeneCollectionRate;
        foreach (var buildRequest in futureBuildRequests) {
            var mineralSpending = 0f;
            var vespeneSpending = 0f;

            if (buildRequest.BuildType == BuildType.Research) {
                var upgradeTypeData = _knowledgeBase.GetUpgradeData(buildRequest.UnitOrUpgradeType);
                mineralSpending += buildRequest.QuantityRemaining * upgradeTypeData.MineralCost;
                vespeneSpending += buildRequest.QuantityRemaining * upgradeTypeData.VespeneCost;
            }
            else {
                var unitTypeData = _knowledgeBase.GetUnitTypeData(buildRequest.UnitOrUpgradeType);
                mineralSpending += buildRequest.QuantityRemaining * unitTypeData.MineralCost;
                vespeneSpending += buildRequest.QuantityRemaining * unitTypeData.VespeneCost;
            }

            var totalSpending = mineralSpending + vespeneSpending;
            var allowedPercent = Math.Min(1, spendingLimit / totalSpending);

            ExpectedFutureMineralsSpending += mineralSpending * allowedPercent;
            ExpectedFutureVespeneSpending += vespeneSpending * allowedPercent;

            spendingLimit -= totalSpending * allowedPercent;

            if (allowedPercent < 1) {
                break;
            }
        }
    }
}
