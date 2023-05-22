using System;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Requests;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class NaiveRayCastClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IAnimationFactory _animationFactory;

    public NaiveRayCastClip(
        ITerrainTracker terrainTracker,
        IAnimationFactory animationFactory,
        Vector2 sceneLocation,
        float stepSize,
        int pauseAtEndOfClipDurationSeconds = 5
    ) : base(animationFactory, pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _animationFactory = animationFactory;

        var centerCameraAnimation = _animationFactory.CreateCenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var showGridEndFrame = ShowGrid(sceneLocation, centerCameraAnimation.AnimationEndFrame);
        NaiveCastRay(sceneLocation, stepSize, showGridEndFrame);
    }

    private int ShowGrid(Vector2 origin, int startAt) {
        var endFrame = startAt;

        var random = new Random();
        foreach (var cell in _terrainTracker.BuildSearchRadius(origin, 15)) {
            var rng = (float)random.NextDouble();
            var rngStartFrame = startAt + (int)TimeUtils.SecsToFrames(rng * 1f);
            var squareAnimation = _animationFactory.CreateCellDrawingAnimation(_terrainTracker.WithWorldHeight(cell), rngStartFrame).WithDurationInSeconds(0.5f);

            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }

    private void NaiveCastRay(Vector2 rayStart, float stepSize, int startFrame) {
        var step = rayStart.Translate(yTranslation: stepSize).RotateAround(rayStart, MathUtils.DegToRad(30)) - rayStart;

        var previousAnimationEnd = startFrame;
        var previousIntersection = rayStart;
        while (_terrainTracker.IsWalkable(previousIntersection)) {
            var rayEnd = previousIntersection + step;
            var lineDrawingAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(previousIntersection), _terrainTracker.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, previousAnimationEnd)
                .WithConstantRate(4);

            AddAnimation(lineDrawingAnimation);

            var sphereDrawingAnimation = _animationFactory.CreateSphereDrawingAnimation(_terrainTracker.WithWorldHeight(rayEnd), 0.1f, ColorService.Instance.PointColor, lineDrawingAnimation.AnimationEndFrame)
                .WithDurationInSeconds(0.2f);

            AddAnimation(sphereDrawingAnimation);

            previousIntersection = rayEnd;
            previousAnimationEnd = sphereDrawingAnimation.AnimationEndFrame;
        }

        var cameraMoveToEndAnimation = _animationFactory.CreateCenterCameraAnimation(previousIntersection, startFrame)
            .WithEndFrame(previousAnimationEnd);

        AddAnimation(cameraMoveToEndAnimation);
    }
}
