using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Utils;

namespace Bot;

public class StuckDetector {
    private const float NegligibleMovement = 2f;
    private static readonly ulong ReasonableMoveDelay = TimeUtils.SecsToFrames(3);

    private ulong _ticksWithoutRealMove = 0;
    private Vector2 _previousArmyLocation;

    public bool IsStuck => _ticksWithoutRealMove > ReasonableMoveDelay;

    public void Tick(Vector2 armyLocation) {
        if (armyLocation.DistanceTo(_previousArmyLocation) < NegligibleMovement) {
            _ticksWithoutRealMove++;
        }
        else {
            Reset(armyLocation);
        }
    }

    public void Reset(Vector2 armyLocation) {
        _ticksWithoutRealMove = 0;
        _previousArmyLocation = armyLocation;
    }
}
