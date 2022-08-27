using System.Collections.Generic;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using SC2APIProtocol;

namespace Bot.Wrapper;

public static class RequestBuilder {
    public static Request RequestAction(IEnumerable<Action> actions) {
        var actionRequest = new Request
        {
            Action = new RequestAction(),
        };

        actionRequest.Action.Actions.AddRange(actions);

        return actionRequest;
    }

    public static Request RequestStep(uint stepSize) {
        return new Request
        {
            Step = new RequestStep
            {
                Count = stepSize,
            }
        };
    }

    public static Request RequestDebug(IEnumerable<DebugText> debugTexts, IEnumerable<DebugSphere> debugSpheres, IEnumerable<DebugBox> debugBoxes, IEnumerable<DebugLine> debugLines) {
        return new Request
        {
            Debug = new RequestDebug
            {
                Debug =
                {
                    new DebugCommand
                    {
                        Draw = new DebugDraw
                        {
                            Text = { debugTexts },
                            Spheres = { debugSpheres },
                            Boxes = { debugBoxes },
                            Lines = { debugLines },
                        },
                    },
                }
            }
        };
    }

    public static Request RequestCreateComputerGame(bool realTime, string mapPath, Race opponentRace, Difficulty opponentDifficulty) {
        var requestCreateGame = new RequestCreateGame
        {
            Realtime = realTime,
            LocalMap = new LocalMap
            {
                MapPath = mapPath,
            }
        };

        requestCreateGame.PlayerSetup.Add(new PlayerSetup
        {
            Type = PlayerType.Participant,
        });

        requestCreateGame.PlayerSetup.Add(new PlayerSetup
        {
            Type = PlayerType.Computer,
            Race = opponentRace,
            Difficulty = opponentDifficulty,
        });

        return new Request
        {
            CreateGame = requestCreateGame,
        };
    }

    public static Request RequestJoinLadderGame(Race race, int startPort) {
        var requestJoinGame = new RequestJoinGame
        {
            Race = race,
            SharedPort = startPort + 1,
            ServerPorts = new PortSet
            {
                GamePort = startPort + 2,
                BasePort = startPort + 3,
            },
            Options = new InterfaceOptions
            {
                Raw = true,
                Score = true,
            }
        };

        requestJoinGame.ClientPorts.Add(new PortSet
        {
            GamePort = startPort + 4,
            BasePort = startPort + 5
        });

        return new Request
        {
            JoinGame = requestJoinGame,
        };
    }

    public static Request RequestJoinLocalGame(Race race) {
        return new Request
        {
            JoinGame = new RequestJoinGame
            {
                Race = race,
                Options = new InterfaceOptions
                {
                    Raw = true,
                    Score = true,
                }
            }
        };
    }

    public static Request RequestQueryBuildingPlacement(uint buildingType, Vector3 position) {
        var requestQuery = new RequestQuery();

        // TODO GD Can I send multiple placements at the same time?
        requestQuery.Placements.Add(new RequestQueryBuildingPlacement
        {
            AbilityId = (int)KnowledgeBase.GetUnitTypeData(buildingType).AbilityId,
            TargetPos = position.ToPoint2D(),
        });

        return new Request
        {
            Query = requestQuery,
        };
    }

    public static Request RequestObservation() {
        return new Request
        {
            Observation = new RequestObservation(),
        };
    }

    public static Request RequestGameInfo() {
        return new Request
        {
            GameInfo = new RequestGameInfo(),
        };
    }

    public static Request DebugCreateUnit(Owner owner, uint unitType, uint quantity, Vector3 position) {
        return new Request
        {
            Debug = new RequestDebug
            {
                Debug =
                {
                    new DebugCommand
                    {
                        CreateUnit = new DebugCreateUnit
                        {
                            Owner = (int)owner,
                            UnitType = unitType,
                            Quantity = quantity,
                            Pos = position.ToPoint2D(),
                        }
                    }
                }
            }
        };
    }
}
