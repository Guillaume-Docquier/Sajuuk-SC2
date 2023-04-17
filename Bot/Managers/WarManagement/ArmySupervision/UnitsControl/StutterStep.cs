using System.Collections.Generic;
using System.Linq;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class StutterStep : IUnitsControl {
    private const bool Debug = true;
    private readonly ExecutionTimeDebugger _debugger = new ExecutionTimeDebugger();

    public bool IsExecuting() {
        return false;
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        if (army.Count <= 0) {
            return army;
        }

        _debugger.Reset();

        _debugger.StartTimer("Total");
        _debugger.StartTimer("Graph");
        var pressureGraph = BuildPressureGraph(army.Where(unit => !unit.IsBurrowed).ToList());
        // We'll print a green graph without cycles on top of the original red graph
        // The only red lines visible will be those associated with a broken cycle
        DebugPressureGraph(pressureGraph, Colors.Red);
        _debugger.StopTimer("Graph");

        _debugger.StartTimer("Cycles");
        BreakCycles(pressureGraph);
        DebugPressureGraph(pressureGraph, Colors.Green);
        _debugger.StopTimer("Cycles");

        _debugger.StartTimer("Moves");
        var uncontrolledUnits = new HashSet<Unit>(army);
        foreach (var unitThatShouldMove in GetUnitsThatShouldMove(pressureGraph)) {
            if (unitThatShouldMove.IsEngagingTheEnemy) {
                unitThatShouldMove.Move(unitThatShouldMove.EngagedTarget.Position.ToVector2());
                uncontrolledUnits.Remove(unitThatShouldMove);
            }

            DebugUnitMove(unitThatShouldMove);
        }
        _debugger.StopTimer("Moves");
        _debugger.StopTimer("Total");

        var totalTime = _debugger.GetExecutionTime("Total");
        if (totalTime >= 5) {
            _debugger.LogExecutionTimes("StutterStep");
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
        var fullyCleared = new HashSet<Unit>();
        foreach (var (soldier, _) in pressureGraph.Where(kv => !kv.Value.To.Any())) {
            var currentBranchSet = new HashSet<Unit>();
            var currentBranchStack = new Stack<Unit>();

            var explorationStack = new Stack<Unit>();
            explorationStack.Push(soldier);

            // Depth first search
            // When backtracking (reaching the end of a branch, no pressure from), clear the currentBranchSet because there were no cycles
            var backtracking = false;
            while (explorationStack.Any()) {
                var toExplore = explorationStack.Pop();

                if (backtracking) {
                    while (!pressureGraph[currentBranchStack.Peek()].From.Contains(toExplore)) {
                        fullyCleared.Add(currentBranchStack.Pop());
                    }

                    currentBranchSet = currentBranchStack.ToHashSet();
                    backtracking = false;
                }

                currentBranchSet.Add(toExplore);
                currentBranchStack.Push(toExplore);

                var leafNode = true;
                foreach (var pressureFrom in pressureGraph[toExplore].From) {
                    if (currentBranchSet.Contains(pressureFrom)) {
                        // Cycle, break it and backtrack
                        pressureGraph[pressureFrom].To.Remove(toExplore);
                        pressureGraph[toExplore].From.Remove(pressureFrom);
                        backtracking = true;
                    }
                    else if (!fullyCleared.Contains(pressureFrom)) {
                        leafNode = false;
                        explorationStack.Push(pressureFrom);
                    }
                }

                // Leaf node, no cycle, let's backtrack
                if (leafNode) {
                    fullyCleared.Add(toExplore);
                    backtracking = true;
                }
            }
        }
    }

    /// <summary>
    /// Get the units that should move based on the pressure graph.
    /// Starting from the leaf nodes (units in the back that have no pressure on them), we traverse the graph an propagate the move intentions.
    /// If a unit needs to move, we ask the units that are being pressured to move as well.
    /// A unit needs to move if:
    /// - The unit has pressure on it and cannot attack (is free to move)
    /// - The unit doesn't have pressure on it but is not in attack range
    ///
    /// The pressure graph will be altered by this method by removing pressure from units that do not need to move.
    /// </summary>
    /// <param name="pressureGraph">The pressure graph to respect</param>
    /// <returns>The list of units that should move given the pressure graph</returns>
    private static IEnumerable<Unit> GetUnitsThatShouldMove(IReadOnlyDictionary<Unit, Pressure> pressureGraph) {
        var checkedUnits = new HashSet<Unit>();
        var unitsThatNeedToMove = new HashSet<Unit>();

        var explorationQueue = new Queue<Unit>();
        // We start with units in the back that are being body blocked
        foreach (var (unit, _) in pressureGraph.Where(kv => !kv.Value.From.Any() && kv.Value.To.Any())) {
            explorationQueue.Enqueue(unit);
        }

        while (explorationQueue.Any()) {
            var soldier = explorationQueue.Dequeue();
            var pressure = pressureGraph[soldier];

            // TODO GD We don't break cycles properly, A->B->A can exist
            // If not all pressurers have been checked, wait
            //if (!pressure.From.All(pressurer => checkedUnits.Contains(pressurer))) {
            //    // This is fine because the pressure graph is a directed acyclic graph
            //    explorationQueue.Enqueue(soldier);
            //    continue;
            //}

            bool shouldMove;
            if (!pressure.From.Any()) {
                // Units in the back should want to move forward if they're not in attack range
                shouldMove = !soldier.IsFightingTheEnemy;
            }
            else {
                // If you have pressure on you, move unless you're ready to attack
                shouldMove = !soldier.IsReadyToAttack;
            }

            DebugUnitMoveCheck(soldier, shouldMove);

            if (shouldMove) {
                unitsThatNeedToMove.Add(soldier);
            }

            // Propagate the move signal forward
            foreach (var pressured in pressure.To) {
                if (!shouldMove) {
                    // We we shouldn't move, remove the pressure
                    pressureGraph[soldier].To.Remove(pressured);
                    pressureGraph[pressured].From.Remove(soldier);
                }

                if (!unitsThatNeedToMove.Contains(pressured)) {
                    explorationQueue.Enqueue(pressured);
                }
            }

            checkedUnits.Add(soldier);
        }

        return unitsThatNeedToMove;
    }

    /// <summary>
    /// Display the pressure graph.
    /// </summary>
    /// <param name="pressureGraph">The pressure graph</param>
    /// <param name="color">The color of the pressure graph</param>
    private static void DebugPressureGraph(IReadOnlyDictionary<Unit, Pressure> pressureGraph, Color color) {
        if (!Debug) {
            return;
        }

        foreach (var (soldier, pressure) in pressureGraph) {
            // Hacky but whatever
            if (color.Equals(Colors.Green)) {
                Program.GraphicalDebugger.AddText($"{pressure.To.Count}-{pressure.From.Count}", worldPos: soldier.Position.ToPoint(yOffset: -0.17f), color: Colors.Yellow);
            }

            foreach (var pressured in pressure.To) {
                Program.GraphicalDebugger.AddArrowedLine(soldier.Position.Translate(zTranslation: 1), pressured.Position.Translate(zTranslation: 1), color);
            }
        }
    }

    private static void DebugUnitMove(Unit unit) {
        if (!Debug) {
            return;
        }

        Program.GraphicalDebugger.AddText("PUSH", worldPos: unit.Position.ToPoint(yOffset: 0.51f));
    }

    private static void DebugUnitMoveCheck(Unit unit, bool shouldMove) {
        if (!Debug) {
            return;
        }

        Program.GraphicalDebugger.AddText($"{shouldMove}", worldPos: unit.Position.ToPoint(yOffset: -0.51f), color: Colors.Yellow);
    }
}

internal struct Pressure {
    public readonly HashSet<Unit> From = new HashSet<Unit>();
    public readonly HashSet<Unit> To = new HashSet<Unit>();

    public Pressure() {}
}
