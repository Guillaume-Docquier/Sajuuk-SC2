using System.Collections.Generic;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using SC2APIProtocol;

namespace Sajuuk.Actions;

public class ActionBuilder : IActionBuilder {
    private readonly KnowledgeBase _knowledgeBase;

    public ActionBuilder(KnowledgeBase knowledgeBase) {
        _knowledgeBase = knowledgeBase;
    }

    public Action TrainUnit(uint unitType, ulong producerTag) {
        var unitAbilityId = _knowledgeBase.GetUnitTypeData(unitType).AbilityId;

        return UnitCommand(unitAbilityId, producerTag);
    }

    public Action PlaceBuilding(uint buildingType, ulong producerTag, Vector2 position) {
        var buildingAbilityId = _knowledgeBase.GetUnitTypeData(buildingType).AbilityId;

        return UnitCommand(buildingAbilityId, producerTag, position: position.ToPoint2D());
    }

    public Action PlaceExtractor(uint buildingType, ulong producerTag, ulong gasTag) {
        var buildingAbilityId = _knowledgeBase.GetUnitTypeData(buildingType).AbilityId;

        return UnitCommand(buildingAbilityId, producerTag, targetUnitTag: gasTag);
    }

    public Action ResearchUpgrade(uint upgradeType, ulong producerTag) {
        var upgradeAbilityId = _knowledgeBase.GetUpgradeData(upgradeType).AbilityId;

        return UnitCommand(upgradeAbilityId, producerTag, queueCommand: true);
    }

    public Action Stop(ulong unitTag) {
        return UnitCommand(Abilities.Stop, unitTag);
    }

    public Action Move(ulong unitTag, Vector2 position) {
        return UnitCommand(Abilities.Move, unitTag, position: position.ToPoint2D());
    }

    // TODO GD Might be better to use a single order
    public Action AttackMove(ulong unitTag, Vector2 position) {
        return UnitCommand(Abilities.Attack, unitTag, position: position.ToPoint2D());
    }

    public Action Attack(ulong unitTag, ulong targetTag) {
        return UnitCommand(Abilities.Attack, unitTag, targetUnitTag: targetTag);
    }

    public Action Smart(ulong unitTag, ulong targetUnitTag) {
        return UnitCommand(Abilities.Smart, unitTag, targetUnitTag: targetUnitTag);
    }

    public Action Gather(ulong unitTag, ulong mineralOrGasTag) {
        return UnitCommand(Abilities.HarvestGather, unitTag, targetUnitTag: mineralOrGasTag);
    }

    public Action ReturnCargo(ulong unitTag) {
        return UnitCommand(Abilities.HarvestReturn, unitTag);
    }

    public Action Chat(string message, bool toTeam = false) {
        return new Action
        {
            ActionChat = new ActionChat
            {
                Channel = toTeam ? ActionChat.Types.Channel.Team : ActionChat.Types.Channel.Broadcast,
                Message = message
            }
        };
    }

    public Action UnitCommand(uint abilityId, ulong unitTag, Point2D position = null, ulong targetUnitTag = ulong.MaxValue, bool queueCommand = false) {
        return UnitCommand(abilityId, new List<ulong> { unitTag }, position, targetUnitTag, queueCommand);
    }

    private static Action UnitCommand(uint abilityId, IEnumerable<ulong> unitTags = null, Point2D position = null, ulong targetUnitTag = ulong.MaxValue, bool queueCommand = false) {
        var action = new Action
        {
            ActionRaw = new ActionRaw
            {
                UnitCommand = new ActionRawUnitCommand
                {
                    AbilityId = (int)abilityId,
                    UnitTags = { unitTags },
                    TargetWorldSpacePos = position,
                    QueueCommand = queueCommand,
                }
            }
        };

        if (targetUnitTag != ulong.MaxValue) {
            action.ActionRaw.UnitCommand.TargetUnitTag = targetUnitTag;
        }

        return action;
    }
}
