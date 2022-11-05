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
    public RaySteppingClip(Vector2 currentCameraLocation, Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds) : base(pauseAtEndOfClipDurationSeconds) {
        sceneLocation = sceneLocation.Translate(xTranslation: -StepTranslation * (Angles.Count - 1));

        var nextAnimationStart = 0;
        foreach (var angle in Angles) {
            var cameraReadyFrame = CenterCamera(currentCameraLocation, sceneLocation, nextAnimationStart, durationInSeconds: 1f);
            nextAnimationStart = RayStep(sceneLocation, angle, cameraReadyFrame);

            currentCameraLocation = sceneLocation;
            sceneLocation = sceneLocation.Translate(xTranslation: StepTranslation);
        }
    }

    private int RayStep(Vector2 rayStart, int angleDeg, int startAt) {
        var rayCastingResults = RayCasting.RayCast(rayStart, MathUtils.DegToRad(angleDeg), cell => !MapAnalyzer.IsWalkable(cell)).ToList();

        var showOriginCellReadyFrame = ShowCell(rayCastingResults[0].CornerOfCell.AsWorldGridCenter(), startAt);
        var showOriginReadyFrame = ShowPoint(rayCastingResults[0].RayIntersection, startAt);
        var castRayReadyFrame = CastRay(rayCastingResults[0].RayIntersection, rayCastingResults[1].RayIntersection, Math.Max(showOriginCellReadyFrame, showOriginReadyFrame));
        var showStepReadyFrame = ShowCell(rayCastingResults[1].CornerOfCell.AsWorldGridCenter(), castRayReadyFrame);

        var pauseAnimation = new PauseAnimation(startFrame: showStepReadyFrame).WithDurationInSeconds(1);
        AddAnimation(pauseAnimation);

        return pauseAnimation.EndFrame;
    }

    private int CastRay(Vector2 rayStart, Vector2 rayEnd, int startAt) {
        var lineDrawingAnimation = new LineDrawingAnimation(rayStart.ToVector3(), rayEnd.ToVector3(), Colors.Green, startAt)
            .WithConstantRate(3);

        AddAnimation(lineDrawingAnimation);

        ShowPoint(rayEnd, lineDrawingAnimation.EndFrame);

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
