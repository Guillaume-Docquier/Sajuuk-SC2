using System.Numerics;
using System.Threading.Tasks;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class CellDrawingAnimation : Animation<CellDrawingAnimation> {
    private readonly Vector3 _cell;
    private readonly Color _cellColor;

    public CellDrawingAnimation(Vector3 cell, int startFrame) : base(startFrame) {
        _cell = cell;
        _cellColor = MapAnalyzer.IsWalkable(_cell) ? Colors.CornflowerBlue : Colors.LightRed;
    }

    protected override Task Animate(int currentClipFrame) {
        var currentDuration = currentClipFrame - StartFrame;
        var percentDone = (float)currentDuration / Duration;
        var squareSide = KnowledgeBase.GameGridCellWidth * percentDone;

        Program.GraphicalDebugger.AddRectangle(_cell, squareSide, squareSide, _cellColor, padded: true);

        return Task.CompletedTask;
    }

    protected override void PostAnimate(int currentClipFrame) {
        Program.GraphicalDebugger.AddRectangle(_cell, KnowledgeBase.GameGridCellWidth, KnowledgeBase.GameGridCellWidth, _cellColor, padded: true);
    }
}
