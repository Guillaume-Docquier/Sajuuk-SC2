using System.Collections.Generic;
using Bot.Builds;
using Bot.GameData;
using SC2APIProtocol;

namespace Bot;

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
    public BuildRequestResult ExecuteBuildStep(BuildFulfillment buildStep);
    public float GetResearchProgress(uint upgradeId);
    public bool IsResearchInProgress(uint upgradeId);
    public IEnumerable<Unit> GetProducersCarryingOrders(uint unitTypeToProduce);
    public IEnumerable<Effect> GetEffects(int effectId);
    public bool IsUnlocked(uint unitOrUpgradeType, Dictionary<uint, List<IPrerequisite>> prerequisites);
    public IEnumerable<Unit> GetMiningTownHalls();
    public Point GetCurrentCameraLocation();
}
