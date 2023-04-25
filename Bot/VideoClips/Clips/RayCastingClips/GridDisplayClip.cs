using System;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class GridDisplayClip : Clip {
    private readonly IMapAnalyzer _mapAnalyzer;

    public GridDisplayClip(IMapAnalyzer mapAnalyzer, Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds = 5): base(pauseAtEndOfClipDurationSeconds) {
        _mapAnalyzer = mapAnalyzer;

        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        ShowGridClosestFirst(sceneLocation, centerCameraAnimation.AnimationEndFrame);
    }

    private int ShowGridClosestFirst(Vector2 origin, int startAt) {
        var grid = _mapAnalyzer.BuildSearchRadius(origin, 15).ToList();
        var maxDistance = grid.Max(cell => cell.DistanceTo(origin));
        var animationTotalDuration = TimeUtils.SecsToFrames(2);

        var endFrame = startAt;
        foreach (var cell in grid) {
            var relativeDistance = cell.DistanceTo(origin) / maxDistance;
            var startFrame = startAt + (int)(relativeDistance * animationTotalDuration);

            var squareAnimation = new CellDrawingAnimation(_mapAnalyzer, _mapAnalyzer.WithWorldHeight(cell), startFrame).WithDurationInSeconds(0.5f);
            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }
}
