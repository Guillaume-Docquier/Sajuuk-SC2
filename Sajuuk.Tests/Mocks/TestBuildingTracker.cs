﻿using System.Numerics;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Tests.Mocks;

public class TestBuildingTracker : IBuildingTracker {
    public Vector2 FindConstructionSpot(uint buildingType) {
        return new Vector2();
    }

    public void ConfirmPlacement(uint buildingType, Vector2 position, Unit builder) {
        return;
    }

    public bool CanPlace(uint buildingType, Vector2 position) {
        return true;
    }

    public ActionResult QueryPlacement(uint buildingType, Vector2 position) {
        return ActionResult.Success;
    }
}
