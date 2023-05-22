using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class ChokeWallsClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IAnimationFactory _animationFactory;

    public ChokeWallsClip(
        ITerrainTracker terrainTracker,
        IAnimationFactory animationFactory,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds
    ) : base(animationFactory, pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _animationFactory = animationFactory;

        var centerCameraAnimation = _animationFactory.CreateCenterCameraAnimation(sceneLocation, startFrame: 0)
            .WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var visibleCells = new HashSet<Vector2>();
        for (var angle = 0; angle < 360; angle += 1) {
            var rayCastResults = RayCasting.RayCast(sceneLocation, MathUtils.DegToRad(angle), cell => !_terrainTracker.IsWalkable(cell)).ToList();
            foreach (var rayCastResult in rayCastResults) {
                visibleCells.Add(rayCastResult.CornerOfCell.AsWorldGridCenter());
            }
        }

        var pauseAnimation = _animationFactory.CreatePauseAnimation(centerCameraAnimation.AnimationEndFrame).WithDurationInSeconds(2);
        AddAnimation(pauseAnimation);

        var showWallsAnimationEndFrame = ShowCells(sceneLocation, visibleCells.Where(cell => !terrainTracker.IsWalkable(cell)).ToList(), pauseAnimation.AnimationEndFrame);
        ShowCells(sceneLocation, visibleCells.Where(cell => terrainTracker.IsWalkable(cell)).ToList(), showWallsAnimationEndFrame);
    }

    private int ShowCells(Vector2 origin, List<Vector2> cells, int startAt) {
        var maxDistance = cells.Max(cell => cell.DistanceTo(origin));
        var animationTotalDuration = TimeUtils.SecsToFrames(10);

        var endFrame = startAt;
        foreach (var cell in cells) {
            var relativeDistance = cell.DistanceTo(origin) / maxDistance;
            var startFrame = startAt + (int)(relativeDistance * animationTotalDuration);

            var squareAnimation = _animationFactory.CreateCellDrawingAnimation(_terrainTracker.WithWorldHeight(cell), startFrame).WithDurationInSeconds(1f);
            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }
}
