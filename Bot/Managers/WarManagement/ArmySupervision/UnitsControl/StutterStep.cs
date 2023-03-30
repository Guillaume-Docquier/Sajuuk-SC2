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
        DebugPressureGraph(pressureGraph, Colors.Red); // Will only be visible if a cycle was broken

        BreakCycles(pressureGraph);
        DebugPressureGraph(pressureGraph, Colors.Green);

        var uncontrolledUnits = new HashSet<Unit>(army);
        foreach (var unitThatShouldMove in GetUnitsThatShouldMove(pressureGraph)) {
            var text = "OK";
            // If you're on cooldown and engaging the enemy, you should push forward
            if (unitThatShouldMove.RawUnitData.WeaponCooldown > 0 && UnitsTracker.UnitsByTag.TryGetValue(unitThatShouldMove.RawUnitData.EngagedTargetTag, out var engagedTarget)) {
                text = "!";
                unitThatShouldMove.Move(engagedTarget.Position.ToVector2());
                uncontrolledUnits.Remove(unitThatShouldMove);
                Controller.SetRealTime("Stutter!");
            }

            Program.GraphicalDebugger.AddText(text, worldPos: unitThatShouldMove.Position.ToPoint(yOffset: 0.30f));
        }

        return uncontrolledUnits;
    }

    public void Reset(IReadOnlyCollection<Unit> army) {}

    private static IReadOnlyDictionary<Unit, Pressure> BuildPressureGraph(IReadOnlyCollection<Unit> uncontrolledUnits) {
        var pressureGraph = uncontrolledUnits.ToDictionary(soldier => soldier, _ => new Pressure());
        foreach (var soldier in uncontrolledUnits) {
            var nextPosition = soldier.Position.ToVector2().TranslateInDirection(soldier.Facing, soldier.Radius * 2);

            // TODO GD That's n^2, can we do better?
            // The army is generally small, maybe it doesn't matter
            var blockingUnits = uncontrolledUnits
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

    private static void BreakCycles(IReadOnlyDictionary<Unit, Pressure> pressureGraph) {
        // Traverse graph to break cycles starting from roots (units in the front that do not pressure anyone)
        foreach (var (soldier, _) in pressureGraph.Where(kv => !kv.Value.To.Any())) {
            // Break cycles
            var currentBranchSet = new HashSet<Unit>();
            var currentBranchStack = new Stack<Unit>();

            var explorationStack = new Stack<Unit>();
            explorationStack.Push(soldier);

            // Depth first search
            // When backtracking (reaching the end of a branch, no pressure from), clear the explored set because there were no cycles
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

    private static IEnumerable<Unit> GetUnitsThatShouldMove(IReadOnlyDictionary<Unit, Pressure> pressureGraph) {
        // Compute pressure values starting from leaves (units in the back that have no pressure on them)
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
