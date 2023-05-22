using System;
using System.Numerics;
using System.Threading.Tasks;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class CellDrawingAnimation : Animation<CellDrawingAnimation> {
    private readonly IGraphicalDebugger _graphicalDebugger;

    private readonly Vector3 _cell;
    private readonly float _padding;
    private readonly Color _cellColor;

    public CellDrawingAnimation(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        Vector3 cell,
        int startFrame,
        float padding
    ) : base(startFrame) {
        _graphicalDebugger = graphicalDebugger;

        _cell = cell;
        _padding = padding;
        _cellColor = terrainTracker.IsWalkable(_cell) ? ColorService.Instance.WalkableCellColor : ColorService.Instance.UnwalkableCellColor;
    }

    protected override Task Animate(int currentClipFrame) {
        var percentDone = GetAnimationPercentDone(currentClipFrame);
        var squareSide = Math.Max(0, KnowledgeBase.GameGridCellWidth - _padding) * percentDone;

        _graphicalDebugger.AddRectangle(_cell, squareSide, squareSide, _cellColor, padded: false);

        return Task.CompletedTask;
    }

    protected override void PostAnimate(int currentClipFrame) {
        var cellWidth = KnowledgeBase.GameGridCellWidth - _padding;
        _graphicalDebugger.AddRectangle(_cell, cellWidth, cellWidth, _cellColor, padded: false);
    }
}
