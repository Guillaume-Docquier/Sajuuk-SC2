using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

// ReSharper disable MemberCanBePrivate.Global

namespace Bot {
    public static class Controller {
        //editable
        private const int FrameDelay = 0; //too fast? increase this to e.g. 20

        //don't edit
        private static readonly List<Action> Actions = new List<Action>();
        private static readonly Random Random = new Random();
        private const double FramesPerSecond = 22.4;

        public static ResponseGameInfo GameInfo;
        public static ResponseData GameData;
        public static ResponseObservation Obs;
        public static ulong Frame;
        public static uint CurrentSupply;
        public static uint MaxSupply;
        public static uint Minerals;
        public static uint Vespene;

        public static readonly List<Vector3> EnemyLocations = new List<Vector3>();
        public static readonly List<string> ChatLog = new List<string>();

        public static void Pause() {
            Console.WriteLine("Press any key to continue...");
            while (Console.ReadKey().Key != ConsoleKey.Enter) {
                //do nothing
            }
        }

        public static ulong SecsToFrames(int seconds) {
            return (ulong)(FramesPerSecond * seconds);
        }

        public static IEnumerable<Action> CloseFrame() {
            return Actions;
        }

        public static void OpenFrame() {
            if (GameInfo == null || GameData == null || Obs == null) {
                if (GameInfo == null) {
                    Logger.Info("GameInfo is null! The application will terminate.");
                }
                else if (GameData == null) {
                    Logger.Info("GameData is null! The application will terminate.");
                }
                else {
                    Logger.Info("ResponseObservation is null! The application will terminate.");
                }

                Pause();
                Environment.Exit(0);
            }

            Actions.Clear();

            foreach (var chat in Obs.Chat) {
                ChatLog.Add(chat.Message);
            }

            Frame = Obs.Observation.GameLoop;
            CurrentSupply = Obs.Observation.PlayerCommon.FoodUsed;
            MaxSupply = Obs.Observation.PlayerCommon.FoodCap;
            Minerals = Obs.Observation.PlayerCommon.Minerals;
            Vespene = Obs.Observation.PlayerCommon.Vespene;

            //initialization
            if (Frame == 0) {
                var resourceCenters = GetUnits(Units.ResourceCenters);
                if (resourceCenters.Count > 0) {
                    var rcPosition = resourceCenters[0].Position;

                    foreach (var startLocation in GameInfo.StartRaw.StartLocations) {
                        var enemyLocation = new Vector3(startLocation.X, startLocation.Y, 0);
                        var distance = Vector3.Distance(enemyLocation, rcPosition);
                        if (distance > 30) {
                            EnemyLocations.Add(enemyLocation);
                        }
                    }
                }
            }

            if (FrameDelay > 0) {
                Thread.Sleep(FrameDelay);
            }
        }

        public static string GetUnitName(uint unitType) {
            return GameData.Units[(int)unitType].Name;
        }

        public static void AddAction(Action action) {
            Actions.Add(action);
        }

        public static void Chat(string message, bool team = false) {
            var action = new Action
            {
                ActionChat = new ActionChat
                {
                    Channel = team ? ActionChat.Types.Channel.Team : ActionChat.Types.Channel.Broadcast,
                    Message = message
                }
            };

            AddAction(action);
        }

        public static void Attack(List<Unit> units, Vector3 target) {
            var action = CreateRawUnitCommand(Abilities.Attack);
            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D
            {
                X = target.X,
                Y = target.Y
            };

            foreach (var unit in units) {
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
            }

            AddAction(action);
        }

        public static int GetTotalCount(uint unitType) {
            var pendingCount = GetPendingCount(unitType, inConstruction: false);
            var constructionCount = GetUnits(unitType).Count;

            return pendingCount + constructionCount;
        }

        public static int GetPendingCount(uint unitType, bool inConstruction = true) {
            var workers = GetUnits(Units.Workers);
            var abilityId = Abilities.GetId(unitType);

            var counter = 0;

            //count workers that have been sent to build this structure
            foreach (var worker in workers) {
                if (worker.Order.AbilityId == abilityId) {
                    counter += 1;
                }
            }

            //count buildings that are already in construction
            if (inConstruction) {
                foreach (var unit in GetUnits(unitType)) {
                    if (unit.BuildProgress < 1) {
                        counter += 1;
                    }
                }
            }

            return counter;
        }

        public static List<Unit> GetUnits(HashSet<uint> hashset, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false) {
            //ideally this should be cached in the future and cleared at each new frame
            var units = new List<Unit>();
            foreach (var unit in Obs.Observation.RawData.Units)
                if (hashset.Contains(unit.UnitType) && unit.Alliance == alliance) {
                    if (onlyCompleted && unit.BuildProgress < 1) {
                        continue;
                    }

                    if (onlyVisible && (unit.DisplayType != DisplayType.Visible)) {
                        continue;
                    }

                    units.Add(new Unit(unit));
                }

            return units;
        }

        public static List<Unit> GetUnits(uint unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false) {
            //ideally this should be cached in the future and cleared at each new frame
            var units = new List<Unit>();
            foreach (var unit in Obs.Observation.RawData.Units)
                if (unit.UnitType == unitType && unit.Alliance == alliance) {
                    if (onlyCompleted && unit.BuildProgress < 1) {
                        continue;
                    }

                    if (onlyVisible && (unit.DisplayType != DisplayType.Visible)) {
                        continue;
                    }

                    units.Add(new Unit(unit));
                }

            return units;
        }

        public static bool CanAfford(uint unitType) {
            var unitData = GameData.Units[(int)unitType];

            return (Minerals >= unitData.MineralCost) && (Vespene >= unitData.VespeneCost);
        }

        public static bool CanConstruct(uint unitType) {
            //is it a structure?
            if (Units.Structures.Contains(unitType)) {
                //we need worker for every structure
                if (GetUnits(Units.Workers).Count == 0) {
                    return false;
                }

                //we need an RC for any structure
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
                if (resourceCenters.Count == 0) {
                    return false;
                }

                if ((unitType == Units.CommandCenter) || (unitType == Units.SupplyDepot)) {
                    return CanAfford(unitType);
                }

                //we need supply depots for the following structures
                var depots = GetUnits(Units.SupplyDepots, onlyCompleted: true);
                if (depots.Count == 0) {
                    return false;
                }

                if (unitType == Units.Barracks) {
                    return CanAfford(unitType);
                }
            }

            //it's an actual unit
            else {
                //do we have enough supply?
                var requiredSupply = Controller.GameData.Units[(int)unitType].FoodRequired;
                if (requiredSupply > (MaxSupply - CurrentSupply)) {
                    return false;
                }

                //do we construct the units from barracks?
                if (Units.FromBarracks.Contains(unitType)) {
                    var barracks = GetUnits(Units.Barracks, onlyCompleted: true);
                    if (barracks.Count == 0) {
                        return false;
                    }
                }
            }

            return CanAfford(unitType);
        }

        public static Action CreateRawUnitCommand(int ability) {
            return new Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = new ActionRawUnitCommand
                    {
                        AbilityId = ability
                    }
                }
            };
        }

        public static bool CanPlace(uint unitType, Vector3 targetPos) {
            //Note: this is a blocking call! Use it sparingly, or you will slow down your execution significantly!
            var abilityId = Abilities.GetId(unitType);

            var queryBuildingPlacement = new RequestQueryBuildingPlacement
            {
                AbilityId = abilityId,
                TargetPos = new Point2D
                {
                    X = targetPos.X,
                    Y = targetPos.Y
                }
            };

            var requestQuery = new Request
            {
                Query = new RequestQuery()
            };
            requestQuery.Query.Placements.Add(queryBuildingPlacement);

            var result = Program.GameConnection.SendQuery(requestQuery.Query);
            if (result.Result.Placements.Count > 0) {
                return (result.Result.Placements[0].Result == ActionResult.Success);
            }

            return false;
        }

        public static void DistributeWorkers() {
            var workers = GetUnits(Units.Workers);
            var idleWorkers = new List<Unit>();
            foreach (var worker in workers) {
                if (worker.Order.AbilityId != 0) {
                    continue;
                }

                idleWorkers.Add(worker);
            }

            if (idleWorkers.Count > 0) {
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
                var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

                foreach (var rc in resourceCenters) {
                    //get one of the closer mineral fields
                    var mf = GetFirstInRange(rc.Position, mineralFields, 7);
                    if (mf == null) {
                        continue;
                    }

                    //only one at a time
                    Logger.Info("Distributing idle worker: {0}", idleWorkers[0].Tag);
                    idleWorkers[0].Smart(mf);

                    return;
                }

                //nothing to be done
                return;
            }
            else {
                //let's see if we can distribute between bases
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
                Unit transferFrom = null;
                Unit transferTo = null;
                foreach (var rc in resourceCenters) {
                    if (rc.AssignedWorkers <= rc.IdealWorkers) {
                        transferTo = rc;
                    }
                    else {
                        transferFrom = rc;
                    }
                }

                if ((transferFrom != null) && (transferTo != null)) {
                    var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);

                    var sqrDistance = 7 * 7;
                    foreach (var worker in workers) {
                        if (worker.Order.AbilityId != Abilities.GatherMinerals) {
                            continue;
                        }

                        if (Vector3.DistanceSquared(worker.Position, transferFrom.Position) > sqrDistance) {
                            continue;
                        }

                        var mf = GetFirstInRange(transferTo.Position, mineralFields, 7);
                        if (mf == null) {
                            continue;
                        }

                        //only one at a time
                        Logger.Info("Distributing idle worker: {0}", worker.Tag);
                        worker.Smart(mf);

                        return;
                    }
                }
            }
        }


        public static Unit GetAvailableWorker(Vector3 targetPosition) {
            var workers = GetUnits(Units.Workers);
            foreach (var worker in workers) {
                if (worker.Order.AbilityId != Abilities.GatherMinerals) {
                    continue;
                }

                return worker;
            }

            return null;
        }

        public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
            return (GetFirstInRange(targetPosition, units, maxDistance) != null);
        }

        public static Unit GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
            //squared distance is faster to calculate
            var maxDistanceSqr = maxDistance * maxDistance;
            foreach (var unit in units) {
                if (Vector3.DistanceSquared(targetPosition, unit.Position) <= maxDistanceSqr) {
                    return unit;
                }
            }

            return null;
        }

        public static void Construct(uint unitType) {
            Vector3 startingSpot;

            var resourceCenters = GetUnits(Units.ResourceCenters);
            if (resourceCenters.Count > 0)
                startingSpot = resourceCenters[0].Position;
            else {
                Logger.Error("Unable to construct: {0}. No resource center was found.", GetUnitName(unitType));

                return;
            }

            const int radius = 12;

            //trying to find a valid construction spot
            var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
            Vector3 constructionSpot;
            while (true) {
                constructionSpot = new Vector3(startingSpot.X + Random.Next(-radius, radius + 1), startingSpot.Y + Random.Next(-radius, radius + 1), 0);

                //avoid building in the mineral line
                if (IsInRange(constructionSpot, mineralFields, 5)) {
                    continue;
                }

                //check if the building fits
                if (!CanPlace(unitType, constructionSpot)) {
                    continue;
                }

                //ok, we found a spot
                break;
            }

            var worker = GetAvailableWorker(constructionSpot);
            if (worker == null) {
                Logger.Error("Unable to find worker to construct: {0}", GetUnitName(unitType));

                return;
            }

            var abilityId = Abilities.GetId(unitType);
            var constructAction = CreateRawUnitCommand(abilityId);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D
            {
                X = constructionSpot.X,
                Y = constructionSpot.Y
            };
            AddAction(constructAction);

            Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(unitType), constructionSpot.X, constructionSpot.Y);
        }
    }
}
