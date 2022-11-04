using System.Numerics;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class LineDrawingAnimation : Animation<LineDrawingAnimation> {
    private readonly Vector3 _lineStart;
    private readonly Vector3 _lineEnd;
    private readonly Color _lineColor;

    private readonly float _lineLength;

    public LineDrawingAnimation(Vector3 lineStart, Vector3 lineEnd, Color lineColor, int startFrame): base(startFrame) {
        _lineStart = lineStart;
        _lineEnd = lineEnd;
        _lineColor = lineColor;

        _lineLength = _lineStart.DistanceTo(_lineEnd);
    }

    protected override Task Animate(int currentClipFrame) {
        var percentDone = GetAnimationPercentDone(currentClipFrame);

        Program.GraphicalDebugger.AddLine(
            _lineStart,
            Vector3.Lerp(_lineStart, _lineEnd, percentDone),
            _lineColor
        );

        return Task.CompletedTask;
    }

    protected override void PostAnimate(int currentFrame) {
        Program.GraphicalDebugger.AddLine(_lineStart, _lineEnd, _lineColor);
    }

    public LineDrawingAnimation WithConstantRate(float unitsPerSecond) {
        Duration = (int)TimeUtils.SecsToFrames(_lineLength / unitsPerSecond);

        return this;
    }
}
