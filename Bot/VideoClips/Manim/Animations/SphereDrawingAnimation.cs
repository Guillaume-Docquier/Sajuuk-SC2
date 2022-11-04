using System.Numerics;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class SphereDrawingAnimation : Animation<SphereDrawingAnimation> {
    private readonly Vector3 _center;
    private readonly float _radius;
    private readonly Color _color;

    public SphereDrawingAnimation(Vector3 center, float radius, Color color, int startFrame) : base(startFrame) {
        _center = center;
        _radius = radius;
        _color = color;
    }

    protected override Task Animate(int currentClipFrame) {
        var currentDuration = currentClipFrame - StartFrame;
        var percentDone = (float)currentDuration / Duration;
        var sphereRadius = _radius * percentDone;

        Program.GraphicalDebugger.AddSphere(_center, sphereRadius, _color);

        return Task.CompletedTask;
    }

    protected override void PostAnimate(int currentClipFrame) {
        Program.GraphicalDebugger.AddSphere(_center, _radius, _color);
    }
}
