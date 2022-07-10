﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Wrapper;

namespace Bot;

public static class MapAnalyzer {
    public static bool IsInitialized = false;

    public const float GameGridCellWidth = 1f;
    public const float GameGridCellRadius = GameGridCellWidth / 2;

    public static List<List<Unit>> ResourceClusters;
    public static List<Vector3> ExpansionLocations;

    private const float ExpandSearchRadius = 5;

    // The controller must be initialized first
    public static void Init() {
        ResourceClusters = FindResourceClusters().ToList();
        Logger.Info("Found {0} resource clusters", ResourceClusters.Count);

        ExpansionLocations = FindExpansionLocations().ToList();
        Logger.Info("Found {0} expansions", ExpansionLocations.Count);

        IsInitialized = true;
    }

    private static IEnumerable<List<Unit>> FindResourceClusters() {
        var minerals = Controller.GetUnits(Controller.NeutralUnits, Units.MineralFields);
        var gasses = Controller.GetUnits(Controller.NeutralUnits, Units.GasGeysers);
        var resources = minerals.Concat(gasses).ToList();

        return Clustering.DBSCAN(resources, epsilon: 7, minPoints: 4);
    }

    private static IEnumerable<Vector3> FindExpansionLocations() {
        var expandLocations = new List<Vector3>();

        foreach (var resourceCluster in ResourceClusters) {
            var centerPosition = GetClusterCenter(resourceCluster);
            var searchGrid = BuildSearchGrid(centerPosition);

            // Search starting with the closest points
            var sortedBuildSpots = searchGrid.OrderBy(position => Vector3.Distance(centerPosition, position));

            var goodBuildSpot = sortedBuildSpots.FirstOrDefault(buildSpot => Controller.CanPlace(Units.Hatchery, buildSpot));
            if (goodBuildSpot != default) {
                expandLocations.Add(goodBuildSpot);
                Debugger.AddSphere(goodBuildSpot.ToPoint(), GameGridCellRadius, Colors.Green);
                Debugger.AddSphere(centerPosition.ToPoint(), GameGridCellRadius, Colors.Yellow);
            }
        }

        return expandLocations;
    }

    private static Vector3 GetClusterCenter(IReadOnlyList<Unit> unitCluster) {
        var minX = unitCluster.Select(unit => unit.Position.X).Min();
        var maxX = unitCluster.Select(unit => unit.Position.X).Max();
        var minY = unitCluster.Select(unit => unit.Position.Y).Min();
        var maxY = unitCluster.Select(unit => unit.Position.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;
        var centerZ = unitCluster[0].Position.Z; // Assume they're all on the same level

        // Sync with building grid
        // Center of cells are on .5, e.g: (1.5, 2.5)
        return new Vector3((float)Math.Floor(centerX) + GameGridCellRadius, (float)Math.Floor(centerY) + GameGridCellRadius, centerZ);
    }

    private static IEnumerable<Vector3> BuildSearchGrid(Vector3 centerPosition) {
        var buildSpots = new List<Vector3>();
        for (var x = centerPosition.X - ExpandSearchRadius; x <= centerPosition.X + ExpandSearchRadius; x += GameGridCellWidth) {
            for (var y = centerPosition.Y - ExpandSearchRadius; y <= centerPosition.Y + ExpandSearchRadius; y += GameGridCellWidth) {
                buildSpots.Add(new Vector3(x, y, centerPosition.Z));
            }
        }

        return buildSpots;
    }
}
