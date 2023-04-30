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

public class RaySteppingClip : Clip {
    private readonly ITerrainTracker _terrainTracker;

    private const int StepTranslation = 4;
    private static readonly List<int> Angles = new List<int> { 25, 45, 65 };
    public RaySteppingClip(ITerrainTracker terrainTracker, Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds) : base(pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;

        sceneLocation = sceneLocation.Translate(xTranslation: -StepTranslation * (Angles.Count - 1));

        var nextAnimationStart = 0;
        foreach (var angle in Angles) {
            var panCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: nextAnimationStart).WithDurationInSeconds(1);
            AddAnimation(panCameraAnimation);

            nextAnimationStart = RayStep(sceneLocation, angle, panCameraAnimation.AnimationEndFrame);
            sceneLocation = sceneLocation.Translate(xTranslation: StepTranslation);
        }
    }

    private int RayStep(Vector2 rayStart, int angleDeg, int startAt) {
        var rayCastingResults = RayCasting.RayCast(rayStart, MathUtils.DegToRad(angleDeg), cell => !_terrainTracker.IsWalkable(cell)).ToList();

        var showOriginCellReadyFrame = ShowCell(rayCastingResults[0].CornerOfCell.AsWorldGridCenter(), startAt);
        var showOriginReadyFrame = ShowPoint(rayCastingResults[0].RayIntersection, startAt);

        var pauseBeforeRayAnimation = new PauseAnimation(Math.Max(showOriginCellReadyFrame, showOriginReadyFrame)).WithDurationInSeconds(0.5f);
        AddAnimation(pauseBeforeRayAnimation);

        var castRayReadyFrame = CastRay(rayCastingResults[0].RayIntersection, rayCastingResults[1].RayIntersection, pauseBeforeRayAnimation.AnimationEndFrame);

        var showStepReadyFrame = ShowCell(rayCastingResults[1].CornerOfCell.AsWorldGridCenter(), castRayReadyFrame);
        var showEndReadyFrame = ShowPoint(rayCastingResults[1].RayIntersection, castRayReadyFrame);

        var pauseBeforeTransitionAnimation = new PauseAnimation(startFrame: Math.Max(showStepReadyFrame, showEndReadyFrame)).WithDurationInSeconds(1);
        AddAnimation(pauseBeforeTransitionAnimation);

        return pauseBeforeTransitionAnimation.AnimationEndFrame;
    }

    private int CastRay(Vector2 rayStart, Vector2 rayEnd, int startAt) {
        var lineDrawingAnimation = new LineDrawingAnimation(_terrainTracker.WithWorldHeight(rayStart), _terrainTracker.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, startAt)
            .WithDurationInSeconds(1);

        AddAnimation(lineDrawingAnimation);

        return lineDrawingAnimation.AnimationEndFrame;
    }

    private int ShowCell(Vector2 cell, int startAt) {
        var squareAnimation = new CellDrawingAnimation(_terrainTracker, _terrainTracker.WithWorldHeight(cell), startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(squareAnimation);

        return squareAnimation.AnimationEndFrame;
    }

    private int ShowPoint(Vector2 origin, int startAt) {
        var sphereAnimation = new SphereDrawingAnimation(_terrainTracker.WithWorldHeight(origin), radius: 0.15f, ColorService.Instance.PointColor, startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(sphereAnimation);

        return sphereAnimation.AnimationEndFrame;
    }
}
