﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class StepComparisonClip : Clip {
    public StepComparisonClip(Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds) : base(pauseAtEndOfClipDurationSeconds) {
        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var gridReadyFrame = ShowGridFarthestFirst(sceneLocation, centerCameraAnimation.AnimationEndFrame);
        CompareSteps(sceneLocation, gridReadyFrame);
    }

    private int ShowGridFarthestFirst(Vector2 origin, int startFrame) {
        var grid = MapAnalyzer.BuildSearchRadius(origin, 15).ToList();
        var maxDistance = grid.Max(cell => cell.DistanceTo(origin));
        var animationTotalDuration = TimeUtils.SecsToFrames(1.5f);

        var endFrame = startFrame;
        foreach (var cell in grid) {
            var relativeDistance = 1 - (cell.DistanceTo(origin) / maxDistance);
            var animationStartFrame = startFrame + (int)(relativeDistance * animationTotalDuration);

            var squareAnimation = new CellDrawingAnimation(cell.ToVector3(), animationStartFrame)
                .WithDurationInSeconds(0.5f);
            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }

    private int CompareSteps(Vector2 origin, int startFrame) {
        var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(30), cell => !MapAnalyzer.IsWalkable(cell)).ToList();

        var currentOrigin = rayCastResults[0].RayIntersection;

        var originPointAnimation = new SphereDrawingAnimation(currentOrigin.ToVector3(), 0.15f, ColorService.Instance.PointColor , startFrame)
            .WithDurationInSeconds(0.5f);
        AddAnimation(originPointAnimation);

        var previousAnimationEndFrame = originPointAnimation.AnimationEndFrame;

        // It doesn't go till the end because nextX or nextY are outside the results of ray casting
        // I don't have the time to fix it so let's just stop before the end
        for (var i = 1; i < rayCastResults.Count - 2; i++) {
            var compareStepAnimationEndFrame = CompareSteps(rayCastResults, currentOrigin, previousAnimationEndFrame);

            var drawStepAnimation = new LineDrawingAnimation(currentOrigin.ToVector3(), rayCastResults[i].RayIntersection.ToVector3(), ColorService.Instance.RayColor, compareStepAnimationEndFrame)
                .WithConstantRate(4);
            AddAnimation(drawStepAnimation);

            var panCameraAnimation = new CenterCameraAnimation(rayCastResults[i].RayIntersection, compareStepAnimationEndFrame)
                .WithEndFrame(drawStepAnimation.AnimationEndFrame + 1);
            AddAnimation(panCameraAnimation);

            var pointAnimation = new SphereDrawingAnimation(rayCastResults[i].RayIntersection.ToVector3(), 0.15f, ColorService.Instance.PointColor , drawStepAnimation.AnimationEndFrame)
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
        var lineToXAxisAnimation = new LineDrawingAnimation(origin.ToVector3(), nextXAxisLineEnd.ToVector3(), Colors.DarkRed, startFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToXAxisAnimation);

        var lineToXIntersectionAnimation = new LineDrawingAnimation(origin.ToVector3(), nextX.RayIntersection.ToVector3(), ColorService.Instance.RayColor, startFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToXIntersectionAnimation);

        var nextY = rayCastResults.First(rayCastResult => (int)rayCastResult.CornerOfCell.Y == (int)origin.AsWorldGridCorner().Y + 1); // We're going up

        var nextYAxisLineEnd = new Vector2(origin.X, nextY.RayIntersection.Y);
        var lineToYAxisAnimation = new LineDrawingAnimation(origin.ToVector3(), nextYAxisLineEnd.ToVector3(), Colors.DarkBlue, lineToXIntersectionAnimation.PostAnimationEndFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToYAxisAnimation);

        var lineToYIntersectionAnimation = new LineDrawingAnimation(origin.ToVector3(), nextY.RayIntersection.ToVector3(), ColorService.Instance.RayColor, lineToXIntersectionAnimation.PostAnimationEndFrame)
            .WithDurationInSeconds(1)
            .WithPostAnimationDurationInSeconds(1);
        AddAnimation(lineToYIntersectionAnimation);

        return lineToYIntersectionAnimation.PostAnimationEndFrame;
    }
}