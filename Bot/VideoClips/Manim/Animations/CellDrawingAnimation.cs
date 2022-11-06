using System;
using System.Numerics;
using System.Threading.Tasks;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class CellDrawingAnimation : Animation<CellDrawingAnimation> {
    private readonly Vector3 _cell;
    private readonly float _padding;
    private readonly Color _cellColor;

    public CellDrawingAnimation(Vector3 cell, int startFrame, float padding = 0f) : base(startFrame) {
        _cell = cell;
        _padding = padding;
        _cellColor = MapAnalyzer.IsWalkable(_cell) ? ColorService.Instance.WalkableCellColor : ColorService.Instance.UnwalkableCellColor;
    }

    protected override Task Animate(int currentClipFrame) {
        var percentDone = GetAnimationPercentDone(currentClipFrame);
        var squareSide = Math.Max(0, KnowledgeBase.GameGridCellWidth - _padding) * percentDone;

        Program.GraphicalDebugger.AddRectangle(_cell, squareSide, squareSide, _cellColor, padded: false);

        return Task.CompletedTask;
    }

    protected override void PostAnimate(int currentClipFrame) {
        var cellWidth = KnowledgeBase.GameGridCellWidth - _padding;
        Program.GraphicalDebugger.AddRectangle(_cell, cellWidth, cellWidth, _cellColor, padded: false);
    }
}
