using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.Wrapper;

public static class ActionBuilder {
    public static Action TrainUnit(uint unitType, ulong producerTag) {
        var unitAbilityId = Abilities.GetId(unitType);

        return CreateRawUnitCommand(unitAbilityId, producerTag);
    }

    public static Action PlaceBuilding(uint buildingType, ulong producerTag, Vector3 position) {
        var buildingAbilityId = Abilities.GetId(buildingType);

        return CreateRawUnitCommand(buildingAbilityId, producerTag, position: new Point2D { X = position.X, Y = position.Y });
    }

    public static Action ResearchTech(int techAbilityId, ulong producerTag) {
        return CreateRawUnitCommand(techAbilityId, producerTag);
    }

    public static Action Move(ulong unitTag, Vector3 position) {
        return CreateRawUnitCommand(Abilities.Move, unitTag, position: new Point2D { X = position.X, Y = position.Y });
    }

    public static Action Attack(IEnumerable<ulong> unitTags, Vector3 position) {
        return CreateRawUnitCommand(Abilities.Attack, unitTags: unitTags, position: new Point2D { X = position.X, Y = position.Y });
    }

    public static Action Smart(ulong unitTag, ulong targetUnitTag) {
        return CreateRawUnitCommand(Abilities.Smart, unitTag, targetUnitTag: targetUnitTag);
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

    private static Action CreateRawUnitCommand(int abilityId, ulong unitTag, Point2D position = null, ulong targetUnitTag = ulong.MaxValue) {
        return CreateRawUnitCommand(abilityId, new List<ulong> { unitTag }, position, targetUnitTag);
    }

    private static Action CreateRawUnitCommand(int abilityId, IEnumerable<ulong> unitTags = null, Point2D position = null, ulong targetUnitTag = ulong.MaxValue) {
        var action = new Action
        {
            ActionRaw = new ActionRaw
            {
                UnitCommand = new ActionRawUnitCommand
                {
                    AbilityId = abilityId,
                    UnitTags = { unitTags },
                    TargetWorldSpacePos = position,
                }
            }
        };

        if (targetUnitTag != ulong.MaxValue) {
            action.ActionRaw.UnitCommand.TargetUnitTag = targetUnitTag;
        }

        return action;
    }
}
