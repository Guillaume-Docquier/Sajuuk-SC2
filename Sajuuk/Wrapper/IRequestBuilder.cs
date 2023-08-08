using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Sajuuk.Wrapper;

public interface IRequestBuilder {
    public Request RequestAction(IEnumerable<Action> actions);
    public Request RequestStep(uint stepSize);
    public Request DebugDraw(IEnumerable<DebugText> debugTexts, IEnumerable<DebugSphere> debugSpheres, IEnumerable<DebugBox> debugBoxes, IEnumerable<DebugLine> debugLines);
    public Request RequestCreateComputerGame(bool realTime, string mapPath, Race opponentRace, Difficulty opponentDifficulty);
    public Request RequestJoinLadderGame(Race race, int startPort);
    public Request RequestJoinLocalGame(Race race);
    public Request RequestQueryBuildingPlacement(uint buildingType, Vector2 position);
    public Request RequestObservation(uint waitUntilFrame);
    public Request RequestGameInfo();
    public Request DebugCreateUnit(UnitOwner unitOwner, uint unitType, uint quantity, Vector3 position);

    /// <summary>
    /// Moves the camera somewhere on the map
    /// </summary>
    /// <param name="moveTo">The point to center the camera on</param>
    /// <returns></returns>
    public Request DebugMoveCamera(Point moveTo);

    /// <summary>
    /// Creates a debug request to reveal the map.
    /// You only need to set this once.
    /// </summary>
    /// <returns>A debug request to reveal the map</returns>
    public Request DebugRevealMap();
}
