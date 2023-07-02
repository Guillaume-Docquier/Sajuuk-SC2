using System.Numerics;
using SC2APIProtocol;

namespace Sajuuk.Actions;

public interface IActionBuilder {
    public Action TrainUnit(uint unitType, ulong producerTag);
    public Action PlaceBuilding(uint buildingType, ulong producerTag, Vector2 position);
    public Action PlaceExtractor(uint buildingType, ulong producerTag, ulong gasTag);
    public Action ResearchUpgrade(uint upgradeType, ulong producerTag);
    public Action Stop(ulong unitTag);
    public Action Move(ulong unitTag, Vector2 position);
    public Action AttackMove(ulong unitTag, Vector2 position);
    public Action Attack(ulong unitTag, ulong targetTag);
    public Action Smart(ulong unitTag, ulong targetUnitTag);
    public Action Gather(ulong unitTag, ulong mineralOrGasTag);
    public Action ReturnCargo(ulong unitTag);
    public Action Chat(string message, bool toTeam = false);
    public Action UnitCommand(uint abilityId, ulong unitTag, Point2D position = null, ulong targetUnitTag = ulong.MaxValue, bool queueCommand = false);
}
