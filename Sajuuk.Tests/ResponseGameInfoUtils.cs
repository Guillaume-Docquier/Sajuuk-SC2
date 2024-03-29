﻿using SC2APIProtocol;

namespace Sajuuk.Tests;

public static class ResponseGameInfoUtils {
    public static uint PlayerId => 1;
    public static uint EnemyId => 2;

    public static ResponseGameInfo CreateResponseGameInfo(int mapWidth = 100, int mapHeight = 100, Race playerRace = Race.Zerg, Race enemyRace = Race.Random) {
        var pathingGrid = Enumerable.Repeat(true, mapWidth * mapHeight).ToList();
        var placementGrid = Enumerable.Repeat(true, mapWidth * mapHeight).ToList();
        var terrainHeight = Enumerable.Repeat(0f, mapWidth * mapHeight).ToList();

        var responseGameInfo = new ResponseGameInfo
        {
            MapName = "UnitTestsMap",
            StartRaw = new StartRaw
            {
                MapSize = new Size2DI
                {
                    X = mapWidth,
                    Y = mapHeight,
                },
                TerrainHeight = new ImageData
                {
                    Data = ImageDataUtils.CreateByeString(terrainHeight),
                },
                PathingGrid = new ImageData
                {
                    Data = ImageDataUtils.CreateByeString(pathingGrid),
                },
                PlacementGrid = new ImageData
                {
                    Data = ImageDataUtils.CreateByeString(placementGrid),
                },
            },
            PlayerInfo =
            {
                new PlayerInfo
                {
                    PlayerId = PlayerId,
                    RaceRequested = playerRace,
                },
                new PlayerInfo
                {
                    PlayerId = EnemyId,
                    RaceRequested = enemyRace,
                },
            }
        };

        var enemySpawn = new Point2D { X = 90.5f, Y = 80.5f };
        responseGameInfo.StartRaw.StartLocations.Add(enemySpawn);

        return responseGameInfo;
    }
}
