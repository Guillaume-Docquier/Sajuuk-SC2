using System.Collections.Generic;
using System.Numerics;
using Bot.GameData;
using SC2APIProtocol;

namespace Bot.Wrapper;

public static class ActionBuilder {
    public static Action TrainUnit(uint unitType, ulong producerTag) {
        var unitAbilityId = KnowledgeBase.GetUnitTypeData(unitType).AbilityId;

        return UnitCommand(unitAbilityId, producerTag);
    }

    public static Action PlaceBuilding(uint buildingType, ulong producerTag, Vector3 position) {
        var buildingAbilityId = KnowledgeBase.GetUnitTypeData(buildingType).AbilityId;

        return UnitCommand(buildingAbilityId, producerTag, position: new Point2D { X = position.X, Y = position.Y });
    }

    public static Action PlaceExtractor(uint buildingType, ulong producerTag, ulong gasTag) {
        var buildingAbilityId = KnowledgeBase.GetUnitTypeData(buildingType).AbilityId;

        return UnitCommand(buildingAbilityId, producerTag, targetUnitTag: gasTag);
    }

    public static Action ResearchUpgrade(uint upgradeType, ulong producerTag) {
        var upgradeAbilityId = KnowledgeBase.GetUpgradeData(upgradeType).AbilityId;

        return UnitCommand(upgradeAbilityId, producerTag, queueCommand: true);
    }

    public static Action Stop(ulong unitTag) {
        return UnitCommand(Abilities.Stop, unitTag);
    }

    public static Action Move(ulong unitTag, Vector3 position) {
        return UnitCommand(Abilities.Move, unitTag, position: new Point2D { X = position.X, Y = position.Y });
    }

    public static Action AttackMove(ulong unitTag, Vector3 position) {
        return UnitCommand(Abilities.Attack, unitTag, position: new Point2D { X = position.X, Y = position.Y });
    }

    // TODO GD Might be better to use a single order
    public static Action AttackMove(IEnumerable<ulong> unitTags, Vector3 position) {
        return UnitCommand(Abilities.Attack, unitTags: unitTags, position: new Point2D { X = position.X, Y = position.Y });
    }

    public static Action Attack(ulong unitTag, ulong targetTag) {
        return UnitCommand(Abilities.Attack, unitTag, targetUnitTag: targetTag);
    }

    public static Action Smart(ulong unitTag, ulong targetUnitTag) {
        return UnitCommand(Abilities.Smart, unitTag, targetUnitTag: targetUnitTag);
    }

    public static Action Gather(ulong unitTag, ulong mineralOrGasTag) {
        return UnitCommand(Abilities.HarvestGather, unitTag, targetUnitTag: mineralOrGasTag);
    }

    public static Action ReturnCargo(ulong unitTag) {
        return UnitCommand(Abilities.HarvestReturn, unitTag);
    }

    public static Action Chat(string message, bool toTeam = false) {
        return new Action
        {
            ActionChat = new ActionChat
            {
                Channel = toTeam ? ActionChat.Types.Channel.Team : ActionChat.Types.Channel.Broadcast,
                Message = message
            }
        };
    }

    public static Action UnitCommand(uint abilityId, ulong unitTag, Point2D position = null, ulong targetUnitTag = ulong.MaxValue, bool queueCommand = false) {
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
