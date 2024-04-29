using System.Numerics;
using SC2APIProtocol;
using SC2Client.GameState;

namespace SC2Client.Debugging.GraphicalDebugging;

/// <summary>
/// A graphical debugger allows for drawing graphics in-game.
/// These graphics are local only. The opponent won't see them, and they won't be shown in the replays either.
/// Very useful for debugging.
/// </summary>
public interface IGraphicalDebugger {
    Request? GetDebugRequest();

    void AddText(string text, uint size = 15, Point? virtualPos = null, Point? worldPos = null, Color? color = null);

    void AddTextGroup(IEnumerable<string> texts, uint size = 15, Point? virtualPos = null, Point? worldPos = null, Color? color = null);

    void AddGridSphere(Vector3 centerPosition, Color? color = null);

    void AddSphere(IUnit unit, Color color);

    void AddSphere(Vector3 position, float radius, Color color);

    void AddGridSquare(Vector3 centerPosition, Color? color = null);

    void AddGridSquaresInRadius(Vector3 centerPosition, int radius, Color color);

    void AddSquare(Vector3 centerPosition, float width, Color color, bool padded = false);

    void AddRectangle(Vector3 centerPosition, float width, float length, Color color, bool padded = false);

    void AddLine(Vector3 start, Vector3 end, Color color);

    void AddDashedLine(Vector3 start, Vector3 end, Color color, float dashLength = 0.25f);

    void AddArrowedLine(Vector3 start, Vector3 end, Color color);

    void AddLink(IUnit start, IUnit end, Color color);

    void AddLink(Vector3 start, Vector3 end, Color color);

    void AddPath(List<Vector3> path, Color startColor, Color endColor);

    void AddUnit(IUnit unit, Color? color = null);
}
