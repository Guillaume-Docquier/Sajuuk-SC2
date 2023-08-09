using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Sajuuk.Debugging.GraphicalDebugging;

/// <summary>
/// The NullGraphicalDebugger does nothing.because on the ladder, we cannot see the screen.
/// Intended to use on the ladder to reduce frame times and memory footprint.
/// </summary>
public class NullGraphicalDebugger: IGraphicalDebugger {
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

    public void AddDashedLine(Vector3 start, Vector3 end, Color color, float dashLength = 0.25f) {}

    public void AddArrowedLine(Vector3 start, Vector3 end, Color color) {}

    public void AddLink(Unit start, Unit end, Color color) {}

    public void AddLink(Vector3 start, Vector3 end, Color color) {}

    public void AddPath(List<Vector2> path, Color startColor, Color endColor) {}

    public void AddPath(List<Vector3> path, Color startColor, Color endColor) {}

    public void AddUnit(Unit unit, Color color = null) {}
}
