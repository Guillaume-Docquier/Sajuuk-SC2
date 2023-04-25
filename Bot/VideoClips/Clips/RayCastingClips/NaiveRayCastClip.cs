using System;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class NaiveRayCastClip : Clip {
    private readonly IMapAnalyzer _mapAnalyzer;

    public NaiveRayCastClip(IMapAnalyzer mapAnalyzer, Vector2 sceneLocation, float stepSize, int pauseAtEndOfClipDurationSeconds = 5) : base(pauseAtEndOfClipDurationSeconds) {
        _mapAnalyzer = mapAnalyzer;

        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var showGridEndFrame = ShowGrid(sceneLocation, centerCameraAnimation.AnimationEndFrame);
        NaiveCastRay(sceneLocation, stepSize, showGridEndFrame);
    }

    private int ShowGrid(Vector2 origin, int startAt) {
        var endFrame = startAt;

        var random = new Random();
        foreach (var cell in _mapAnalyzer.BuildSearchRadius(origin, 15)) {
            var rng = (float)random.NextDouble();
            var rngStartFrame = startAt + (int)TimeUtils.SecsToFrames(rng * 1f);
            var squareAnimation = new CellDrawingAnimation(_mapAnalyzer, _mapAnalyzer.WithWorldHeight(cell), rngStartFrame).WithDurationInSeconds(0.5f);

            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }

    private void NaiveCastRay(Vector2 rayStart, float stepSize, int startFrame) {
        var step = rayStart.Translate(yTranslation: stepSize).RotateAround(rayStart, MathUtils.DegToRad(30)) - rayStart;

        var previousAnimationEnd = startFrame;
        var previousIntersection = rayStart;
        while (_mapAnalyzer.IsWalkable(previousIntersection)) {
            var rayEnd = previousIntersection + step;
            var lineDrawingAnimation = new LineDrawingAnimation(_mapAnalyzer.WithWorldHeight(previousIntersection), _mapAnalyzer.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, previousAnimationEnd)
                .WithConstantRate(4);

            AddAnimation(lineDrawingAnimation);

            var sphereDrawingAnimation = new SphereDrawingAnimation(_mapAnalyzer.WithWorldHeight(rayEnd), 0.1f, ColorService.Instance.PointColor, lineDrawingAnimation.AnimationEndFrame)
                .WithDurationInSeconds(0.2f);

            AddAnimation(sphereDrawingAnimation);

            previousIntersection = rayEnd;
            previousAnimationEnd = sphereDrawingAnimation.AnimationEndFrame;
        }

        var cameraMoveToEndAnimation = new CenterCameraAnimation(previousIntersection, startFrame)
            .WithEndFrame(previousAnimationEnd);

        AddAnimation(cameraMoveToEndAnimation);
    }
}
