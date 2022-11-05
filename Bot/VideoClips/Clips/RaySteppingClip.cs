using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class RaySteppingClip : Clip {
    private const int StepTranslation = 4;
    private static readonly List<int> Angles = new List<int> { 25, 45, 65 };
    public RaySteppingClip(Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds) : base(pauseAtEndOfClipDurationSeconds) {
        sceneLocation = sceneLocation.Translate(xTranslation: -StepTranslation * (Angles.Count - 1));

        var nextAnimationStart = 0;
        foreach (var angle in Angles) {
            var panCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: nextAnimationStart).WithDurationInSeconds(1);
            AddAnimation(panCameraAnimation);

            nextAnimationStart = RayStep(sceneLocation, angle, panCameraAnimation.EndFrame);
            sceneLocation = sceneLocation.Translate(xTranslation: StepTranslation);
        }
    }

    private int RayStep(Vector2 rayStart, int angleDeg, int startAt) {
        var rayCastingResults = RayCasting.RayCast(rayStart, MathUtils.DegToRad(angleDeg), cell => !MapAnalyzer.IsWalkable(cell)).ToList();

        var showOriginCellReadyFrame = ShowCell(rayCastingResults[0].CornerOfCell.AsWorldGridCenter(), startAt);
        var showOriginReadyFrame = ShowPoint(rayCastingResults[0].RayIntersection, startAt);

        var pauseBeforeRayAnimation = new PauseAnimation(Math.Max(showOriginCellReadyFrame, showOriginReadyFrame)).WithDurationInSeconds(0.5f);
        AddAnimation(pauseBeforeRayAnimation);

        var castRayReadyFrame = CastRay(rayCastingResults[0].RayIntersection, rayCastingResults[1].RayIntersection, pauseBeforeRayAnimation.EndFrame);

        var showStepReadyFrame = ShowCell(rayCastingResults[1].CornerOfCell.AsWorldGridCenter(), castRayReadyFrame);
        var showEndReadyFrame = ShowPoint(rayCastingResults[1].RayIntersection, castRayReadyFrame);

        var pauseBeforeTransitionAnimation = new PauseAnimation(startFrame: Math.Max(showStepReadyFrame, showEndReadyFrame)).WithDurationInSeconds(1);
        AddAnimation(pauseBeforeTransitionAnimation);

        return pauseBeforeTransitionAnimation.EndFrame;
    }

    private int CastRay(Vector2 rayStart, Vector2 rayEnd, int startAt) {
        var lineDrawingAnimation = new LineDrawingAnimation(rayStart.ToVector3(), rayEnd.ToVector3(), Colors.Green, startAt)
            .WithDurationInSeconds(1);

        AddAnimation(lineDrawingAnimation);

        return lineDrawingAnimation.EndFrame;
    }

    private int ShowCell(Vector2 cell, int startAt) {
        var squareAnimation = new CellDrawingAnimation(cell.ToVector3(), startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(squareAnimation);

        return squareAnimation.EndFrame;
    }

    private int ShowPoint(Vector2 origin, int startAt) {
        var sphereAnimation = new SphereDrawingAnimation(origin.ToVector3(), radius: 0.15f, Colors.Purple, startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(sphereAnimation);

        return sphereAnimation.EndFrame;
    }
}
