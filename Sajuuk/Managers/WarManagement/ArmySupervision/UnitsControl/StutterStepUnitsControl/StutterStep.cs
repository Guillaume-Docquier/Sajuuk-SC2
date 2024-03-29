﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging;
using Sajuuk.Debugging.GraphicalDebugging;
using SC2APIProtocol;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

public class StutterStep : IUnitsControl {
    private readonly IGraphicalDebugger _graphicalDebugger;

    [Flags]
    private enum DebugModeFlags {
        None = 0,
        PressureArrows = 1,
        Move = 2,
        MoveCheck = 4,
        Pressures = 8,
        All = 16 - 1,
    }

    private const DebugModeFlags DebugMode = DebugModeFlags.PressureArrows | DebugModeFlags.Move;

    private readonly ExecutionTimeDebugger _debugger = new ExecutionTimeDebugger();

    public StutterStep(IGraphicalDebugger graphicalDebugger) {
        _graphicalDebugger = graphicalDebugger;
    }

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
        DebugPressureArrows(pressureGraph, Colors.Red);
        _debugger.StopTimer("Graph");

        _debugger.StartTimer("Cycles");
        PressureGraph.BreakCycles(pressureGraph);
        DebugPressureArrows(pressureGraph, Colors.Green);
        _debugger.StopTimer("Cycles");

        _debugger.StartTimer("Moves");
        DebugPressures(pressureGraph);
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
    private static IReadOnlyDictionary<Unit, PressureGraph.Pressure<Unit>> BuildPressureGraph(IReadOnlyCollection<Unit> army) {
        var pressureGraph = army.ToDictionary(soldier => soldier, _ => new PressureGraph.Pressure<Unit>());
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
    private IEnumerable<Unit> GetUnitsThatShouldMove(IReadOnlyDictionary<Unit, PressureGraph.Pressure<Unit>> pressureGraph) {
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

            // If not all pressurers have been checked, wait
            if (!pressure.From.All(pressurer => checkedUnits.Contains(pressurer))) {
                // This is fine because the pressure graph is a directed acyclic graph
                explorationQueue.Enqueue(soldier);
                continue;
            }

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
    private void DebugPressureArrows(IReadOnlyDictionary<Unit, PressureGraph.Pressure<Unit>> pressureGraph, Color color) {
        if (!DebugMode.HasFlag(DebugModeFlags.PressureArrows)) {
            return;
        }

        foreach (var (soldier, pressure) in pressureGraph) {
            foreach (var pressured in pressure.To) {
                _graphicalDebugger.AddArrowedLine(soldier.Position.Translate(zTranslation: 1), pressured.Position.Translate(zTranslation: 1), color);
            }
        }
    }

    private void DebugPressures(IReadOnlyDictionary<Unit, PressureGraph.Pressure<Unit>> pressureGraph) {
        if (!DebugMode.HasFlag(DebugModeFlags.Pressures)) {
            return;
        }

        foreach (var (soldier, pressure) in pressureGraph) {
            _graphicalDebugger.AddText($"{pressure.To.Count}-{pressure.From.Count}", worldPos: soldier.Position.ToPoint(yOffset: -0.17f), color: Colors.Yellow);
        }
    }

    private void DebugUnitMove(Unit unit) {
        if (!DebugMode.HasFlag(DebugModeFlags.Move)) {
            return;
        }

        _graphicalDebugger.AddText("PUSH", worldPos: unit.Position.ToPoint(yOffset: 0.51f));
    }

    private void DebugUnitMoveCheck(Unit unit, bool shouldMove) {
        if (!DebugMode.HasFlag(DebugModeFlags.MoveCheck)) {
            return;
        }

        _graphicalDebugger.AddText($"{shouldMove}", worldPos: unit.Position.ToPoint(yOffset: -0.51f), color: Colors.Yellow);
    }
}
