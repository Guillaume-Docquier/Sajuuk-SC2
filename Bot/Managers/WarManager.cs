﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.Managers.ArmyManagement;
using Bot.MapKnowledge;

namespace Bot.Managers;

public class WarManager: IManager {
    private const int GuardDistance = 8;
    private const int GuardRadius = 8;
    private const int AttackRadius = 999; // Basically the whole map
    private const int ForceRequiredBeforeAttacking = 18;

    private bool _hasAssaultStarted = false;

    private readonly ArmyManager _armyManager;
    private Unit _townHallToDefend;

    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep>();
    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests;

    public WarManager() {
        var townHallDefensePosition = GetTownHallDefensePosition(Controller.StartingTownHall, Controller.EnemyLocations[0]);
        _armyManager = new ArmyManager();
        _armyManager.Assign(townHallDefensePosition, GuardRadius, false);
        _townHallToDefend = Controller.StartingTownHall;
    }

    // TODO GD Use queens?
    // TODO GD Use multiple managers, probably
    public void OnFrame() {
        var newSoldiers = Controller.GetUnits(Controller.NewOwnedUnits, Units.ZergMilitary).ToList();
        newSoldiers.ForEach(soldier => soldier.Manager = this);
        _armyManager.Assign(newSoldiers);

        var enemyPosition = Controller.EnemyLocations[0];

        if (!_hasAssaultStarted) {
            DefendNewTownHalls(enemyPosition);
        }

        if (!_hasAssaultStarted && _armyManager.Army.GetForce() >= ForceRequiredBeforeAttacking) {
            StartTheAssault(enemyPosition);
        }

        _armyManager.OnFrame();
    }

    public void Release(Unit unit) {
        _armyManager.Release(unit);
    }

    private void DefendNewTownHalls(Vector3 enemyPosition) {
        var currentDistanceToEnemy = Pathfinder.FindPath(_townHallToDefend.Position, enemyPosition).Count; // Not exact, but the distance difference should not matter
        var newTownHallToDefend = Controller.GetUnits(Controller.NewOwnedUnits, Units.Hatchery)
            .FirstOrDefault(townHall => Pathfinder.FindPath(townHall.Position, enemyPosition).Count < currentDistanceToEnemy);

        // TODO GD Fallback on other townhalls when destroyed
        if (newTownHallToDefend != default) {
            _armyManager.Assign(GetTownHallDefensePosition(newTownHallToDefend, Controller.EnemyLocations[0]), GuardRadius, false);
            _townHallToDefend = newTownHallToDefend;
        }
    }

    private void StartTheAssault(Vector3 enemyPosition) {
        _hasAssaultStarted = true;
        _armyManager.Assign(enemyPosition, AttackRadius);

        // TODO GD Handle this better
        if (_buildStepRequests.Count == 0) {
            _buildStepRequests.Add(new BuildOrders.BuildStep(BuildType.Train, 0, Units.Roach, 1000));
        }
    }

    public void Retire() {
        throw new NotImplementedException();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        // Nothing to do
    }

    private static Vector3 GetTownHallDefensePosition(Unit townHall, Vector3 threatPosition) {
        var pathToThreat = Pathfinder.FindPath(townHall.Position, threatPosition);
        var guardDistance = Math.Min(pathToThreat.Count, GuardDistance);

        return pathToThreat[guardDistance];
    }
}
