using System;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class GridDisplayClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    public GridDisplayClip(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds = 5
    ) : base(pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;

        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        ShowGridClosestFirst(sceneLocation, centerCameraAnimation.AnimationEndFrame);
    }

    private int ShowGridClosestFirst(Vector2 origin, int startAt) {
        var grid = _terrainTracker.BuildSearchRadius(origin, 15).ToList();
        var maxDistance = grid.Max(cell => cell.DistanceTo(origin));
        var animationTotalDuration = TimeUtils.SecsToFrames(2);

        var endFrame = startAt;
        foreach (var cell in grid) {
            var relativeDistance = cell.DistanceTo(origin) / maxDistance;
            var startFrame = startAt + (int)(relativeDistance * animationTotalDuration);

            var squareAnimation = new CellDrawingAnimation(_terrainTracker, _graphicalDebugger, _terrainTracker.WithWorldHeight(cell), startFrame).WithDurationInSeconds(0.5f);
            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }
}
