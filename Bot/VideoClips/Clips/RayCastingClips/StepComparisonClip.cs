using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Requests;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class StepComparisonClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IAnimationFactory _animationFactory;

    public StepComparisonClip(
        ITerrainTracker terrainTracker,
        IAnimationFactory animationFactory,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds
    ) : base(animationFactory, pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _animationFactory = animationFactory;

        var centerCameraAnimation = _animationFactory.CreateCenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var gridReadyFrame = ShowGridFarthestFirst(sceneLocation, centerCameraAnimation.AnimationEndFrame);
        CompareSteps(sceneLocation, gridReadyFrame);
    }

    private int ShowGridFarthestFirst(Vector2 origin, int startFrame) {
        var grid = _terrainTracker.BuildSearchRadius(origin, 15).ToList();
        var maxDistance = grid.Max(cell => cell.DistanceTo(origin));
        var animationTotalDuration = TimeUtils.SecsToFrames(1.5f);

        var endFrame = startFrame;
        foreach (var cell in grid) {
            var relativeDistance = 1 - (cell.DistanceTo(origin) / maxDistance);
            var animationStartFrame = startFrame + (int)(relativeDistance * animationTotalDuration);

            var squareAnimation = _animationFactory.CreateCellDrawingAnimation(_terrainTracker.WithWorldHeight(cell), animationStartFrame)
                .WithDurationInSeconds(0.5f);
            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }

    private int CompareSteps(Vector2 origin, int startFrame) {
        var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(30), cell => !_terrainTracker.IsWalkable(cell)).ToList();

        var currentOrigin = rayCastResults[0].RayIntersection;

        var originPointAnimation = _animationFactory.CreateSphereDrawingAnimation(_terrainTracker.WithWorldHeight(currentOrigin), 0.15f, ColorService.Instance.PointColor , startFrame)
            .WithDurationInSeconds(0.5f);
        AddAnimation(originPointAnimation);

        var previousAnimationEndFrame = originPointAnimation.AnimationEndFrame;

        // It doesn't go till the end because nextX or nextY are outside the results of ray casting
        // I don't have the time to fix it so let's just stop before the end
        for (var i = 1; i < rayCastResults.Count - 2; i++) {
            var compareStepAnimationEndFrame = CompareSteps(rayCastResults, currentOrigin, previousAnimationEndFrame);

            var drawStepAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(currentOrigin), _terrainTracker.WithWorldHeight(rayCastResults[i].RayIntersection), ColorService.Instance.RayColor, compareStepAnimationEndFrame)
                .WithConstantRate(4);
            AddAnimation(drawStepAnimation);

            var panCameraAnimation = _animationFactory.CreateCenterCameraAnimation(rayCastResults[i].RayIntersection, compareStepAnimationEndFrame)
                .WithEndFrame(drawStepAnimation.AnimationEndFrame + 1);
            AddAnimation(panCameraAnimation);

            var pointAnimation = _animationFactory.CreateSphereDrawingAnimation(_terrainTracker.WithWorldHeight(rayCastResults[i].RayIntersection), 0.15f, ColorService.Instance.PointColor , drawStepAnimation.AnimationEndFrame)
                .WithDurationInSeconds(0.5f);
            AddAnimation(pointAnimation);

            currentOrigin = rayCastResults[i].RayIntersection;
            previousAnimationEndFrame = pointAnimation.AnimationEndFrame;
        }

        return previousAnimationEndFrame;
    }

    private int CompareSteps(IReadOnlyCollection<RayCasting.RayCastResult> rayCastResults, Vector2 origin, int startFrame) {
        var nextX = rayCastResults.First(rayCastResult => (int)rayCastResult.CornerOfCell.X == (int)origin.AsWorldGridCorner().X - 1); // We're going left

        // Not sure why and I don't have time to fix it properly
        if (nextX.RayIntersection == origin) {
            nextX = rayCastResults.First(rayCastResult => (int)rayCastResult.CornerOfCell.X == (int)origin.AsWorldGridCorner().X - 2);
        }

        var nextXAxisLineEnd = new Vector2(nextX.RayIntersection.X, origin.Y);
        var lineToXAxisAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(origin), _terrainTracker.WithWorldHeight(nextXAxisLineEnd), Colors.DarkRed, startFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToXAxisAnimation);

        var lineToXIntersectionAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(origin), _terrainTracker.WithWorldHeight(nextX.RayIntersection), ColorService.Instance.RayColor, startFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToXIntersectionAnimation);

        var nextY = rayCastResults.First(rayCastResult => (int)rayCastResult.CornerOfCell.Y == (int)origin.AsWorldGridCorner().Y + 1); // We're going up

        var nextYAxisLineEnd = new Vector2(origin.X, nextY.RayIntersection.Y);
        var lineToYAxisAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(origin), _terrainTracker.WithWorldHeight(nextYAxisLineEnd), Colors.DarkBlue, lineToXIntersectionAnimation.PostAnimationEndFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToYAxisAnimation);

        var lineToYIntersectionAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(origin), _terrainTracker.WithWorldHeight(nextY.RayIntersection), ColorService.Instance.RayColor, lineToXIntersectionAnimation.PostAnimationEndFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToYIntersectionAnimation);

        return lineToYIntersectionAnimation.PostAnimationEndFrame;
    }
}
