using System.Numerics;
using System.Threading.Tasks;
using Bot.Debugging.GraphicalDebugging;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class SphereDrawingAnimation : Animation<SphereDrawingAnimation> {
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly Vector3 _center;
    private readonly float _radius;
    private readonly Color _color;

    public SphereDrawingAnimation(
        IGraphicalDebugger graphicalDebugger,
        Vector3 center,
        float radius,
        Color color,
        int startFrame
    ) : base(startFrame) {
        _graphicalDebugger = graphicalDebugger;
        _center = center;
        _radius = radius;
        _color = color;
    }

    protected override Task Animate(int currentClipFrame) {
        var percentDone = GetAnimationPercentDone(currentClipFrame);
        var sphereRadius = _radius * percentDone;

        _graphicalDebugger.AddSphere(_center, sphereRadius, _color);

        return Task.CompletedTask;
    }

    protected override void PostAnimate(int currentClipFrame) {
        _graphicalDebugger.AddSphere(_center, _radius, _color);
    }
}
