﻿using System.Numerics;
using System.Threading.Tasks;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.Utils;
using SC2APIProtocol;

namespace Sajuuk.VideoClips.Manim.Animations;

public class LineDrawingAnimation : Animation<LineDrawingAnimation> {
    private const float ThicknessOffset = 0.005f;

    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly Vector3 _lineStart;
    private readonly Vector3 _lineEnd;
    private readonly Color _color;
    private readonly int _thickness;

    private readonly float _lineLength;

    public LineDrawingAnimation(
        IGraphicalDebugger graphicalDebugger,
        Vector3 lineStart,
        Vector3 lineEnd,
        Color color,
        int startFrame,
        int thickness
    ) : base(startFrame) {
        _graphicalDebugger = graphicalDebugger;

        _lineStart = lineStart;
        _lineEnd = lineEnd;
        _color = color;
        _thickness = thickness;

        _lineLength = _lineStart.DistanceTo(_lineEnd);
    }

    protected override Task Animate(int currentClipFrame) {
        var percentDone = GetAnimationPercentDone(currentClipFrame);

        for (var offset = -_thickness; offset <= _thickness; offset++) {
            var start = _lineStart.TranslateTowards(_lineEnd, ThicknessOffset * offset).Rotate2D(MathUtils.DegToRad(90), _lineStart);
            var end = _lineEnd.TranslateTowards(_lineStart, ThicknessOffset * offset).Rotate2D(MathUtils.DegToRad(-90), _lineEnd);

            _graphicalDebugger.AddLine(
                start,
                Vector3.Lerp(start, end, percentDone),
                _color
            );
        }

        return Task.CompletedTask;
    }

    protected override void PostAnimate(int currentFrame) {
        for (var offset = -_thickness; offset <= _thickness; offset++) {
            var start = _lineStart.TranslateTowards(_lineEnd, ThicknessOffset * offset).Rotate2D(MathUtils.DegToRad(90), _lineStart);
            var end = _lineEnd.TranslateTowards(_lineStart, ThicknessOffset * offset).Rotate2D(MathUtils.DegToRad(-90), _lineEnd);

            _graphicalDebugger.AddLine(start, end, _color);
        }
    }

    public LineDrawingAnimation WithConstantRate(float unitsPerSecond) {
        AnimationDuration = (int)TimeUtils.SecsToFrames(_lineLength / unitsPerSecond);

        return this;
    }
}
