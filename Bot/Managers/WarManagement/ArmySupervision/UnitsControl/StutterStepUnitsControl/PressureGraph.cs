using System.Collections.Generic;
using System.Linq;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

public static class PressureGraph {
    public struct Pressure<TItem> {
        public readonly HashSet<TItem> From = new HashSet<TItem>();
        public readonly HashSet<TItem> To = new HashSet<TItem>();

        public Pressure() {}
    }

    /// <summary>
    /// Break cycles in the given pressure graph.
    /// We will do a depth first traversal of the graph starting from the "root" nodes, that is the units in the front that do not pressure anyone.
    /// While traversing, we keep track of the current branch and if we visit a member of the branch twice before reaching the end, we cut the last edge.
    ///
    /// The pressure graph will be mutated.
    ///
    /// </summary>
    /// <param name="pressureGraph">The pressure graph to break the cycles of</param>
    /// <returns>The list of all removed pressures.</returns>
    // TODO GD This is probably the dirtiest code of the project. Admittedly, I am tired. However, I'm not lazy, and the code is tested!
    // TODO GD This is a case of "if it ain't broke, don't fix it"!
    public static List<Pressure<TItem>> BreakCycles<TItem>(IReadOnlyDictionary<TItem, Pressure<TItem>> pressureGraph) {
        var removedPressures = new List<Pressure<TItem>>();

        var fullyCleared = new HashSet<TItem>();

        // Start from the start
        foreach (var (item, _) in pressureGraph.Where(kv => !kv.Value.From.Any())) {
            var currentBranchSet = new HashSet<TItem>();
            var currentBranchStack = new Stack<TItem>();

            var explorationStack = new Stack<TItem>();
            explorationStack.Push(item);

            // Depth first search
            // When backtracking (reaching the end of a branch, no pressure from), clear the currentBranchSet because there were no cycles
            var backtracking = false;
            while (explorationStack.Any()) {
                var toExplore = explorationStack.Pop();

                if (backtracking) {
                    while (!pressureGraph[currentBranchStack.Peek()].To.Contains(toExplore)) {
                        fullyCleared.Add(currentBranchStack.Pop());
                    }

                    currentBranchSet = currentBranchStack.ToHashSet();
                    backtracking = false;
                }

                currentBranchSet.Add(toExplore);
                currentBranchStack.Push(toExplore);

                var leafNode = true;
                foreach (var pressureFrom in pressureGraph[toExplore].To) {
                    if (currentBranchSet.Contains(pressureFrom)) {
                        // Cycle, break it and backtrack
                        pressureGraph[pressureFrom].From.Remove(toExplore);
                        pressureGraph[toExplore].To.Remove(pressureFrom);

                        removedPressures.Add(new Pressure<TItem>
                        {
                            To = { pressureFrom },
                            From = { toExplore },
                        });

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

        // Start from the end
        foreach (var (item, _) in pressureGraph.Where(kv => !fullyCleared.Contains(kv.Key) && !kv.Value.To.Any())) {
            var currentBranchSet = new HashSet<TItem>();
            var currentBranchStack = new Stack<TItem>();

            var explorationStack = new Stack<TItem>();
            explorationStack.Push(item);

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

                        removedPressures.Add(new Pressure<TItem>
                        {
                            From = { pressureFrom },
                            To = { toExplore },
                        });

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

        // Clear remaining cycles
        var remainingCycles = pressureGraph.Keys.Except(fullyCleared);
        foreach (var item in remainingCycles) {
            var currentBranchSet = new HashSet<TItem>();
            var currentBranchStack = new Stack<TItem>();

            var explorationStack = new Stack<TItem>();
            explorationStack.Push(item);

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

                        removedPressures.Add(new Pressure<TItem>
                        {
                            From = { pressureFrom },
                            To = { toExplore },
                        });

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

        return removedPressures;
    }
}
