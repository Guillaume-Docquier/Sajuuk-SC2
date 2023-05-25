using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class AnimationFactory : IAnimationFactory {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IController _controller;
    private readonly IRequestBuilder _requestBuilder;
    private readonly ISc2Client _sc2Client;

    public AnimationFactory(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IController controller,
        IRequestBuilder requestBuilder,
        ISc2Client sc2Client
    ) {
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _controller = controller;
        _requestBuilder = requestBuilder;
        _sc2Client = sc2Client;
    }

    public CellDrawingAnimation CreateCellDrawingAnimation(Vector3 cell, int startFrame, float padding = 0f) {
        return new CellDrawingAnimation(_terrainTracker, _graphicalDebugger, cell, startFrame, padding);
    }

    public CenterCameraAnimation CreateCenterCameraAnimation(Vector2 destination, int startFrame) {
        return new CenterCameraAnimation(_controller, _requestBuilder, _sc2Client, destination, startFrame);
    }

    public LineDrawingAnimation CreateLineDrawingAnimation(Vector3 lineStart, Vector3 lineEnd, Color color, int startFrame, int thickness = 3) {
        return new LineDrawingAnimation(_graphicalDebugger, lineStart, lineEnd, color, startFrame, thickness);
    }

    public MoveCameraAnimation CreateMoveCameraAnimation(Vector2 origin, Vector2 destination, int startFrame) {
        return new MoveCameraAnimation(_requestBuilder, _sc2Client, origin, destination, startFrame);
    }

    public PauseAnimation CreatePauseAnimation(int startFrame) {
        return new PauseAnimation(startFrame);
    }

    public SphereDrawingAnimation CreateSphereDrawingAnimation(Vector3 center, float radius, Color color, int startFrame) {
        return new SphereDrawingAnimation(_graphicalDebugger, center, radius, color, startFrame);
    }
}
