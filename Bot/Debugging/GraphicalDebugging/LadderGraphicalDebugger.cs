using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.Debugging.GraphicalDebugging;

/// <summary>
/// The LadderGraphicalDebugger is essentially a void GraphicalDebugger because on the ladder, we cannot see the screen.
/// </summary>
public class LadderGraphicalDebugger: IGraphicalDebugger {
    public Request GetDebugRequest() {
        return null;
    }

    public void AddText(string text, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null) {}

    public void AddTextGroup(IEnumerable<string> texts, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null) {}

    public void AddGridSphere(Vector3 centerPosition, Color color = null) {}

    public void AddSphere(Unit unit, Color color) {}

    public void AddSphere(Vector3 position, float radius, Color color) {}

    public void AddGridSquare(Vector3 centerPosition, Color color = null) {}

    public void AddGridSquaresInRadius(Vector3 centerPosition, int radius, Color color) {}

    public void AddSquare(Vector3 centerPosition, float width, Color color, bool padded = false) {}

    public void AddRectangle(Vector3 centerPosition, float width, float length, Color color, bool padded = false) {}

    public void AddLine(Vector3 start, Vector3 end, Color color) {}

    public void AddLink(Unit start, Unit end, Color color, bool withText = true) {}

    public void AddLink(Vector3 start, Vector3 end, Color color, bool withText = true) {}

    public void AddPath(List<Vector3> path, Color startColor, Color endColor) {}

    public void AddUnit(Unit unit, Color color = null) {}
}
