using System.Numerics;
using SC2APIProtocol;

namespace Sajuuk.VideoClips.Manim.Animations;

public interface IAnimationFactory {
    public CellDrawingAnimation CreateCellDrawingAnimation(Vector3 cell, int startFrame, float padding = 0f);
    public CenterCameraAnimation CreateCenterCameraAnimation(Vector2 destination, int startFrame);
    public LineDrawingAnimation CreateLineDrawingAnimation(Vector3 lineStart, Vector3 lineEnd, Color color, int startFrame, int thickness = 3);
    public MoveCameraAnimation CreateMoveCameraAnimation(Vector2 origin, Vector2 destination, int startFrame);
    public PauseAnimation CreatePauseAnimation(int startFrame);
    public SphereDrawingAnimation CreateSphereDrawingAnimation(Vector3 center, float radius, Color color, int startFrame);
}
