using System.Numerics;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class LineDrawingAnimation : Animation<LineDrawingAnimation> {
    private readonly Vector3 _lineStart;
    private readonly Vector3 _lineEnd;
    private readonly Color _lineColor;

    public LineDrawingAnimation(Vector3 lineStart, Vector3 lineEnd, Color lineColor, int startFrame): base(startFrame) {
        _lineStart = lineStart;
        _lineEnd = lineEnd;
        _lineColor = lineColor;
    }

    protected override void Animate(int currentClipFrame) {
        var currentDuration = currentClipFrame - StartFrame;
        var percentDone = (float)currentDuration / Duration;

        Program.GraphicalDebugger.AddLine(
            _lineStart,
            Vector3.Lerp(_lineStart, _lineEnd, percentDone),
            _lineColor
        );
    }

    protected override void PostAnimate(int currentFrame) {
        Program.GraphicalDebugger.AddLine(_lineStart, _lineEnd, _lineColor);
    }
}
