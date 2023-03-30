using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.Debugging.GraphicalDebugging;

public interface IGraphicalDebugger {
    Request GetDebugRequest();

    void AddText(string text, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null);

    void AddTextGroup(IEnumerable<string> texts, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null);

    void AddGridSphere(Vector3 centerPosition, Color color = null);

    void AddSphere(Unit unit, Color color);

    void AddSphere(Vector3 position, float radius, Color color);

    void AddGridSquare(Vector3 centerPosition, Color color = null);

    void AddGridSquaresInRadius(Vector3 centerPosition, int radius, Color color);

    void AddSquare(Vector3 centerPosition, float width, Color color, bool padded = false);

    void AddRectangle(Vector3 centerPosition, float width, float length, Color color, bool padded = false);

    void AddLine(Vector3 start, Vector3 end, Color color);

    void AddArrowedLine(Vector3 start, Vector3 end, Color color);

    void AddLink(Unit start, Unit end, Color color);

    void AddLink(Vector3 start, Vector3 end, Color color);

    void AddPath(List<Vector2> path, Color startColor, Color endColor);

    void AddPath(List<Vector3> path, Color startColor, Color endColor);

    void AddUnit(Unit unit, Color color = null);
}
