using System;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class RayCastingIntersectionsClip : Clip {
    private readonly IMapAnalyzer _mapAnalyzer;

    public RayCastingIntersectionsClip(IMapAnalyzer mapAnalyzer, Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds = 5): base(pauseAtEndOfClipDurationSeconds) {
        _mapAnalyzer = mapAnalyzer;

        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var gridReadyFrame = ShowGrid(sceneLocation, centerCameraAnimation.AnimationEndFrame);
        CastRay(sceneLocation, gridReadyFrame);
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

    private void CastRay(Vector2 origin, int startAt) {
        var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(30), cell => !_mapAnalyzer.IsWalkable(cell)).ToList();

        var previousIntersection = rayCastResults[0].RayIntersection;
        var previousAnimationEnd = startAt;
        foreach (var rayCastResult in rayCastResults) {
            var rayEnd = rayCastResult.RayIntersection;
            var lineDrawingAnimation = new LineDrawingAnimation(_mapAnalyzer.WithWorldHeight(previousIntersection), _mapAnalyzer.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, previousAnimationEnd)
                .WithConstantRate(4);

            AddAnimation(lineDrawingAnimation);

            var sphereDrawingAnimation = new SphereDrawingAnimation(_mapAnalyzer.WithWorldHeight(rayEnd), 0.1f, ColorService.Instance.PointColor, lineDrawingAnimation.AnimationEndFrame)
                .WithDurationInSeconds(0.5f);

            AddAnimation(sphereDrawingAnimation);

            previousIntersection = rayCastResult.RayIntersection;
            previousAnimationEnd = sphereDrawingAnimation.AnimationEndFrame + (int)TimeUtils.SecsToFrames(0.5f);
        }

        var cameraMoveToEndAnimation = new CenterCameraAnimation(rayCastResults.Last().RayIntersection, startAt)
            .WithEndFrame(previousAnimationEnd);

        AddAnimation(cameraMoveToEndAnimation);
    }
}
