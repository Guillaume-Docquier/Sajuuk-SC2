﻿using System;
using System.Linq;
using Bot.GameData;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class GasMiningStrategy: IStrategy {
    private static readonly ulong GasDeathDelay = Convert.ToUInt64(1.415 * Controller.FramesPerSecond) + 5; // +5 just to be sure

    private readonly Unit _worker;
    private readonly Unit _resource;

    public GasMiningStrategy(Unit worker, Unit resource) {
        // Workers disappear when going inside extractors for 1.415 seconds
        // Change their death delay so that we don't think they're dead
        worker.DeathDelay = GasDeathDelay;

        _worker = worker;
        _resource = resource;
    }

    public void Execute() {
        if (!_worker.Orders.Any()) {
            _worker.Gather(_resource);
        }
        else if (_worker.Orders.Any(order => order.AbilityId == Abilities.DroneGather && order.TargetUnitTag != _resource.Tag)) {
            _worker.Gather(_resource);
        }

        GraphicalDebugger.AddLine(_worker.Position, _resource.Position, Colors.Lime);
    }
}
