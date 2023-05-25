using System.Collections.Generic;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using SC2APIProtocol;

namespace Bot.Wrapper;

public class RequestBuilder : IRequestBuilder {
    private readonly KnowledgeBase _knowledgeBase;

    public RequestBuilder(KnowledgeBase knowledgeBase) {
        _knowledgeBase = knowledgeBase;
    }

    public Request RequestAction(IEnumerable<Action> actions) {
        var actionRequest = new Request
        {
            Action = new RequestAction(),
        };

        actionRequest.Action.Actions.AddRange(actions);

        return actionRequest;
    }

    public Request RequestStep(uint stepSize) {
        return new Request
        {
            Step = new RequestStep
            {
                Count = stepSize,
            }
        };
    }

    public Request DebugDraw(IEnumerable<DebugText> debugTexts, IEnumerable<DebugSphere> debugSpheres, IEnumerable<DebugBox> debugBoxes, IEnumerable<DebugLine> debugLines) {
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

    public Request RequestCreateComputerGame(bool realTime, string mapPath, Race opponentRace, Difficulty opponentDifficulty) {
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

    public Request RequestJoinLadderGame(Race race, int startPort) {
        var requestJoinGame = new RequestJoinGame
        {
            Race = race,
            SharedPort = startPort + 1,
            ServerPorts = new PortSet
            {
                GamePort = startPort + 2,
                BasePort = startPort + 3,
            },
            Options = GetInterfaceOptions(),
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

    public Request RequestJoinLocalGame(Race race) {
        return new Request
        {
            JoinGame = new RequestJoinGame
            {
                Race = race,
                Options = GetInterfaceOptions(),
            }
        };
    }

    private static InterfaceOptions GetInterfaceOptions() {
        return new InterfaceOptions
        {
            Raw = true,
            Score = true,
            ShowCloaked = true,
            ShowBurrowedShadows = true,
            // ShowPlaceholders = true, // TODO GD Consider enabling this to simplify the BuildingsTracker?
        };
    }

    public Request RequestQueryBuildingPlacement(uint buildingType, Vector2 position) {
        var requestQuery = new RequestQuery();

        // TODO GD Can I send multiple placements at the same time?
        requestQuery.Placements.Add(new RequestQueryBuildingPlacement
        {
            AbilityId = (int)_knowledgeBase.GetUnitTypeData(buildingType).AbilityId,
            TargetPos = position.ToPoint2D(),
        });

        return new Request
        {
            Query = requestQuery,
        };
    }

    public Request RequestObservation(uint waitUntilFrame) {
        return new Request
        {
            Observation = new RequestObservation
            {
                GameLoop = waitUntilFrame,
            },
        };
    }

    public Request RequestGameInfo() {
        return new Request
        {
            GameInfo = new RequestGameInfo(),
        };
    }

    public Request DebugCreateUnit(Owner owner, uint unitType, uint quantity, Vector3 position) {
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

    /// <summary>
    /// Moves the camera somewhere on the map
    /// </summary>
    /// <param name="moveTo">The point to center the camera on</param>
    /// <returns></returns>
    public Request DebugMoveCamera(Point moveTo) {
        var moveCameraAction = new Action
        {
            ActionRaw = new ActionRaw
            {
                CameraMove = new ActionRawCameraMove
                {
                    CenterWorldSpace = moveTo,
                }
            }
        };

        return RequestAction(new List<Action> { moveCameraAction });
    }

    /// <summary>
    /// Creates a debug request to reveal the map.
    /// You only need to set this once.
    /// </summary>
    /// <returns>A debug request to reveal the map</returns>
    public Request DebugRevealMap() {
        return new Request
        {
            Debug = new RequestDebug
            {
                Debug =
                {
                    new DebugCommand
                    {
                        GameState = DebugGameState.ShowMap,
                    }
                }
            }
        };
    }
}
