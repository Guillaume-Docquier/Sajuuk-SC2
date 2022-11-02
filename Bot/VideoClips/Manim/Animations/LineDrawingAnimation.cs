using System.Numerics;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class LineDrawingAnimation : IAnimation {
    private readonly Vector3 _lineStart;
    private readonly Vector3 _lineEnd;
    private readonly Color _lineColor;

    public int StartFrame { get; }
    private int _duration = 0;
    public int EndFrame => StartFrame + _duration;

    public LineDrawingAnimation(Vector3 lineStart, Vector3 lineEnd, Color lineColor, int startFrame) {
        _lineStart = lineStart;
        _lineEnd = lineEnd;
        _lineColor = lineColor;

        StartFrame = startFrame;
    }

    public LineDrawingAnimation WithDurationInFrames(int frameDuration) {
        _duration = frameDuration;

        return this;
    }

    public LineDrawingAnimation WithDurationInSeconds(int secondsDuration) {
        _duration = (int)TimeUtils.SecsToFrames(secondsDuration);

        return this;
    }

    public LineDrawingAnimation WithEndFrame(int endFrame) {
        _duration = endFrame - StartFrame;

        return this;
    }

    public void Render(int currentClipFrame) {
        if (currentClipFrame < StartFrame) {
            return;
        }

        if (currentClipFrame > EndFrame) {
            Program.GraphicalDebugger.AddLine(_lineStart, _lineEnd, _lineColor);
        }
        else {
            Animate(currentClipFrame);
        }
    }

    private void Animate(int currentClipFrame) {
        var currentDuration = currentClipFrame - StartFrame;
        var percentDone = (float)currentDuration / _duration;

        Program.GraphicalDebugger.AddLine(
            _lineStart,
            Vector3.Lerp(_lineStart, _lineEnd, percentDone),
            _lineColor
        );
    }
}
