using System.Collections.Generic;
using System.Linq;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class StutterStep : IUnitsControl {
    public bool IsExecuting() {
        return false;
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        if (army.Count <= 0) {
            return army;
        }

        var pressureGraph = BuildPressureGraph(army.Where(unit => !unit.IsBurrowed).ToList());
        // We'll print a green graph without cycles on top of the original red graph
        // The only red lines visible will be those associated with a broken cycle
        DebugPressureGraph(pressureGraph, Colors.Red);

        BreakCycles(pressureGraph);
        DebugPressureGraph(pressureGraph, Colors.Green);

        var uncontrolledUnits = new HashSet<Unit>(army);
        foreach (var unitThatShouldMove in GetUnitsThatShouldMove(pressureGraph)) {
            var unitStatus = "OK";
            // If you're on cooldown and engaging the enemy, you should push forward
            // - If you're not on cooldown we'll let you attack
            // - If you're not engaging an enemy, we'll let the others decide who you should attack
            if (unitThatShouldMove.RawUnitData.WeaponCooldown > 0 && UnitsTracker.UnitsByTag.TryGetValue(unitThatShouldMove.RawUnitData.EngagedTargetTag, out var engagedTarget)) {
                unitStatus = "MOVE";
                unitThatShouldMove.Move(engagedTarget.Position.ToVector2());
                uncontrolledUnits.Remove(unitThatShouldMove);
                Controller.SetRealTime("Stutter!");
            }

            if (unitThatShouldMove.RawUnitData.WeaponCooldown <= 0) {
                unitStatus = "ATK";
            }

            Program.GraphicalDebugger.AddText(unitStatus, worldPos: unitThatShouldMove.Position.ToPoint(yOffset: 0.30f));
        }

        return uncontrolledUnits;
    }

    public void Reset(IReadOnlyCollection<Unit> army) {}

    /// <summary>
    /// Build a directed pressure graph to represent units that want to move but that are being blocked by other units.
    /// We will consider that a unit is blocked by another one if translating the unit forwards would result in a collision with any other unit in the army.
    /// When a unit is blocked by another one, we will say that it pressures the blocking unit.
    /// The graph may contain cycles.
    /// </summary>
    /// <param name="army">The units to consider</param>
    /// <returns>The pressure graph</returns>
    private static IReadOnlyDictionary<Unit, Pressure> BuildPressureGraph(IReadOnlyCollection<Unit> army) {
        var pressureGraph = army.ToDictionary(soldier => soldier, _ => new Pressure());
        foreach (var soldier in army) {
            var nextPosition = soldier.Position.ToVector2().TranslateInDirection(soldier.Facing, soldier.Radius * 2);

            // TODO GD That's n^2, can we do better?
            // The army is generally small, maybe it doesn't matter
            var blockingUnits = army
                .Where(otherSoldier => otherSoldier != soldier)
                .Where(otherSoldier => otherSoldier.DistanceTo(nextPosition) < otherSoldier.Radius + soldier.Radius)
                .ToList();

            foreach (var blockingUnit in blockingUnits) {
                pressureGraph[soldier].To.Add(blockingUnit);
                pressureGraph[blockingUnit].From.Add(soldier);
            }
        }

        return pressureGraph;
    }

    /// <summary>
    /// Break cycles in the given pressure graph.
    /// We will do a depth first traversal of the graph starting from the "root" nodes, that is the units in the front that do not pressure anyone.
    /// While traversing, we keep track of the current branch and if we visit a member of the branch twice before reaching the end, we cut the last edge.
    ///
    /// The pressure graph will be mutated.
    /// </summary>
    /// <param name="pressureGraph">The pressure graph to break the cycles of</param>
    private static void BreakCycles(IReadOnlyDictionary<Unit, Pressure> pressureGraph) {
        foreach (var (soldier, _) in pressureGraph.Where(kv => !kv.Value.To.Any())) {
            var currentBranchSet = new HashSet<Unit>();
            var currentBranchStack = new Stack<Unit>();

            var explorationStack = new Stack<Unit>();
            explorationStack.Push(soldier);

            // Depth first search
            // When backtracking (reaching the end of a branch, no pressure from), clear the currentBranchSet because there were no cycles
            var bracktracking = false;
            while (explorationStack.Any()) {
                var toExplore = explorationStack.Pop();

                if (bracktracking) {
                    while (!pressureGraph[currentBranchStack.Peek()].From.Contains(toExplore)) {
                        currentBranchStack.Pop();
                    }

                    currentBranchSet = currentBranchStack.ToHashSet();
                    bracktracking = false;
                }

                currentBranchSet.Add(toExplore);
                currentBranchStack.Push(toExplore);

                foreach (var pressureFrom in pressureGraph[toExplore].From) {
                    if (currentBranchSet.Contains(pressureFrom)) {
                        // Cycle, break it and backtrack
                        pressureGraph[pressureFrom].To.Remove(toExplore);
                        pressureGraph[toExplore].From.Remove(pressureFrom);
                        bracktracking = true;
                    }
                    else {
                        explorationStack.Push(pressureFrom);
                    }
                }

                // Leaf node, no cycle, let's backtrack
                if (!pressureGraph[toExplore].From.Any()) {
                    bracktracking = true;
                }
            }
        }
    }

    /// <summary>
    /// Get the units that should move based on the pressure graph.
    /// Starting from the leaf nodes (units in the back that have no pressure on them), we traverse the graph an propagate the move intentions.
    /// If a unit needs to move, we ask the units that are being pressures on to move as well.
    /// A unit "needs to move" if all of these are true
    /// - It has an order that makes it move
    /// - It is pressuring other units
    /// - It is not engaging an enemy
    ///
    /// A unit will be pressured to move of all of these are true
    /// - A unit is pressuring it
    /// - It is engaging an enemy
    /// - It is on cooldown
    /// </summary>
    /// <param name="pressureGraph">The pressure graph to respect</param>
    /// <returns>The list of units that should move given the pressure graph</returns>
    private static IEnumerable<Unit> GetUnitsThatShouldMove(IReadOnlyDictionary<Unit, Pressure> pressureGraph) {
        var unitsThatNeedToMove = new HashSet<Unit>();

        var explorationQueue = new Queue<(Unit unit, Pressure pressure)>();
        foreach (var (unit, pressure) in pressureGraph.Where(kv => !kv.Value.From.Any())) {
            explorationQueue.Enqueue((unit, pressure));
        }

        while (explorationQueue.Any()) {
            var (soldier, pressure) = explorationQueue.Dequeue();
            var shouldMove = false;

            // You need to move if you're attacking but not engaging
            // TODO GD More orders can probably cause you to want to move
            shouldMove |= pressure.To.Any() && (soldier.IsMoving() || soldier.IsAttacking()) && !UnitsTracker.UnitsByTag.ContainsKey(soldier.RawUnitData.EngagedTargetTag);

            // You need to move if you're pressured and engaging but on cooldown
            // TODO GD Maybe you should be considered moving regardless of cooldown (we just won't ask you to move, but the move propagation will happen)
            shouldMove |= pressure.From.Any() && UnitsTracker.UnitsByTag.ContainsKey(soldier.RawUnitData.EngagedTargetTag) && soldier.RawUnitData.WeaponCooldown > 0;

            if (!shouldMove) {
                continue;
            }

            unitsThatNeedToMove.Add(soldier);
            foreach (var pressured in pressure.To.Where(pressured => !unitsThatNeedToMove.Contains(pressured))) {
                explorationQueue.Enqueue((pressured, pressureGraph[pressured]));
            }
        }

        return unitsThatNeedToMove;
    }

    /// <summary>
    /// Display the pressure graph.
    /// </summary>
    /// <param name="pressureGraph">The pressure graph</param>
    /// <param name="color">The color of the pressure graph</param>
    private static void DebugPressureGraph(IReadOnlyDictionary<Unit, Pressure> pressureGraph, Color color) {
        foreach (var (soldier, pressure) in pressureGraph) {
            foreach (var pressured in pressure.To) {
                Program.GraphicalDebugger.AddArrowedLine(soldier.Position.Translate(zTranslation: 1), pressured.Position.Translate(zTranslation: 1), color);
            }
        }
    }
}

internal struct Pressure {
    public readonly HashSet<Unit> From = new HashSet<Unit>();
    public readonly HashSet<Unit> To = new HashSet<Unit>();

    public Pressure() {}
}
