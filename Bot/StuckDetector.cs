using System.Numerics;
using Bot.ExtensionMethods;

namespace Bot;

public class StuckDetector {
    private const float NegligibleMovement = 2f;
    private static readonly ulong ReasonableMoveDelay = Controller.SecsToFrames(3);

    private ulong _ticksWithoutRealMove = 0;
    private Vector3 _previousArmyLocation;

    public bool IsStuck => _ticksWithoutRealMove > ReasonableMoveDelay;

    public void Tick(Vector3 armyLocation) {
        if (armyLocation.HorizontalDistanceTo(_previousArmyLocation) < NegligibleMovement) {
            _ticksWithoutRealMove++;
        }
        else {
            Reset(armyLocation);
        }
    }

    public void Reset(Vector3 armyLocation) {
        _ticksWithoutRealMove = 0;
        _previousArmyLocation = armyLocation;
    }
}
