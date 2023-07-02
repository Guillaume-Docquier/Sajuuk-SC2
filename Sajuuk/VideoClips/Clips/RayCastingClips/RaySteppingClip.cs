using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.GameSense;
using Sajuuk.Utils;
using Sajuuk.VideoClips.Manim.Animations;

namespace Sajuuk.VideoClips.Clips.RayCastingClips;

public class RaySteppingClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IAnimationFactory _animationFactory;

    private const int StepTranslation = 4;
    private static readonly List<int> Angles = new List<int> { 25, 45, 65 };
    public RaySteppingClip(
        ITerrainTracker terrainTracker,
        IAnimationFactory animationFactory,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds
    ) : base(animationFactory, pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _animationFactory = animationFactory;

        sceneLocation = sceneLocation.Translate(xTranslation: -StepTranslation * (Angles.Count - 1));

        var nextAnimationStart = 0;
        foreach (var angle in Angles) {
            var panCameraAnimation = _animationFactory.CreateCenterCameraAnimation(sceneLocation, startFrame: nextAnimationStart).WithDurationInSeconds(1);
            AddAnimation(panCameraAnimation);

            nextAnimationStart = RayStep(sceneLocation, angle, panCameraAnimation.AnimationEndFrame);
            sceneLocation = sceneLocation.Translate(xTranslation: StepTranslation);
        }
    }

    private int RayStep(Vector2 rayStart, int angleDeg, int startAt) {
        var rayCastingResults = RayCasting.RayCast(rayStart, MathUtils.DegToRad(angleDeg), cell => !_terrainTracker.IsWalkable(cell)).ToList();

        var showOriginCellReadyFrame = ShowCell(rayCastingResults[0].CornerOfCell.AsWorldGridCenter(), startAt);
        var showOriginReadyFrame = ShowPoint(rayCastingResults[0].RayIntersection, startAt);

        var pauseBeforeRayAnimation = _animationFactory.CreatePauseAnimation(Math.Max(showOriginCellReadyFrame, showOriginReadyFrame)).WithDurationInSeconds(0.5f);
        AddAnimation(pauseBeforeRayAnimation);

        var castRayReadyFrame = CastRay(rayCastingResults[0].RayIntersection, rayCastingResults[1].RayIntersection, pauseBeforeRayAnimation.AnimationEndFrame);

        var showStepReadyFrame = ShowCell(rayCastingResults[1].CornerOfCell.AsWorldGridCenter(), castRayReadyFrame);
        var showEndReadyFrame = ShowPoint(rayCastingResults[1].RayIntersection, castRayReadyFrame);

        var pauseBeforeTransitionAnimation = _animationFactory.CreatePauseAnimation(startFrame: Math.Max(showStepReadyFrame, showEndReadyFrame)).WithDurationInSeconds(1);
        AddAnimation(pauseBeforeTransitionAnimation);

        return pauseBeforeTransitionAnimation.AnimationEndFrame;
    }

    private int CastRay(Vector2 rayStart, Vector2 rayEnd, int startAt) {
        var lineDrawingAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(rayStart), _terrainTracker.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, startAt)
            .WithDurationInSeconds(1);

        AddAnimation(lineDrawingAnimation);

        return lineDrawingAnimation.AnimationEndFrame;
    }

    private int ShowCell(Vector2 cell, int startAt) {
        var squareAnimation = _animationFactory.CreateCellDrawingAnimation(_terrainTracker.WithWorldHeight(cell), startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(squareAnimation);

        return squareAnimation.AnimationEndFrame;
    }

    private int ShowPoint(Vector2 origin, int startAt) {
        var sphereAnimation = _animationFactory.CreateSphereDrawingAnimation(_terrainTracker.WithWorldHeight(origin), radius: 0.15f, ColorService.Instance.PointColor, startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(sphereAnimation);

        return sphereAnimation.AnimationEndFrame;
    }
}
