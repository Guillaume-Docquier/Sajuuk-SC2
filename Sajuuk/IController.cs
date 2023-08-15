using System.Collections.Generic;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameData;
using SC2APIProtocol;

namespace Sajuuk;

// TODO GD This interface is dirty, nothing should be added to it.
// TODO GD We should progressively remove stuff from here
public interface IController {
    public ResponseGameInfo GameInfo { get; }
    public ResponseObservation Observation { get; }
    public uint SupportedSupply { get; }
    public uint CurrentSupply { get; }
    public int AvailableSupply { get; }

    public bool IsSupplyBlocked { get; }

    public int AvailableMinerals { get; }
    public int AvailableVespene { get; }

    public HashSet<uint> ResearchedUpgrades { get; }
    public void SetSimulationTime(string reason);
    public void SetRealTime(string reason);
    public void NewFrame(ResponseGameInfo gameInfo, ResponseObservation observation);

    /// <summary>
    /// Determines if we can spend the given amounts of minerals and vespene.
    /// </summary>
    /// <param name="mineralCost">The amount of minerals we'd like to spend.</param>
    /// <param name="vespeneCost">The amount of vespene we'd like to spend.</param>
    /// <returns>True if we have enough resources to spend the given amount of minerals and vespene.</returns>
    public BuildRequestResult CanAfford(int mineralCost, int vespeneCost);

    /// <summary>
    /// Determines if we can use the given amount of food (supply).
    /// </summary>
    /// <param name="foodCost">The amount of food (supply) that we'd like to spend.</param>
    /// <returns>True if we have enough resources to spend the given amount of food (supply).</returns>
    public bool HasEnoughSupply(float foodCost);

    /// <summary>
    /// Spends the given resources.
    /// </summary>
    /// <param name="mineralCost">The amount of minerals to spend.</param>
    /// <param name="vespeneCost">The amount of vespene to spend.</param>
    /// <param name="foodCost">The amount of food (supply) to spend.</param>
    /// <returns>True if we could afford the given resources.</returns>
    public bool Spend(int mineralCost = 0, int vespeneCost = 0, float foodCost = 0);

    public float GetResearchProgress(uint upgradeId);
    public bool IsResearchInProgress(uint upgradeId);
    public IEnumerable<Unit> GetProducersCarryingOrders(uint unitTypeToProduce);
    public IEnumerable<Effect> GetEffects(int effectId);

    /// <summary>
    /// Determines if the given unit or upgrade type is unlocked, given its set of pre-requisites.
    /// </summary>
    /// <param name="unitOrUpgradeType">The type of the unit or upgrade to check.</param>
    /// <param name="prerequisites">The prerequisites for that unit or upgrade.</param>
    /// <returns>True if all the prerequisites are met.</returns>
    public bool IsUnlocked(uint unitOrUpgradeType, Dictionary<uint, List<IPrerequisite>> prerequisites);

    public IEnumerable<Unit> GetMiningTownHalls();
    public Point GetCurrentCameraLocation();
}
