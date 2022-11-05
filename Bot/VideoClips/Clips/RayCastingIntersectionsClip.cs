using System;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class RayCastingIntersectionsClip : Clip {
    private readonly int _pauseAtEndOfClipDurationSeconds;

    public RayCastingIntersectionsClip(Vector2 currentCameraLocation, Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds = 5): base(0) {
        _pauseAtEndOfClipDurationSeconds = pauseAtEndOfClipDurationSeconds;

        var cameraReadyFrame = CenterCamera(currentCameraLocation, sceneLocation);
        var gridReadyFrame = ShowGrid(sceneLocation, cameraReadyFrame);
        CastRay(sceneLocation, gridReadyFrame);
    }

    private void CastRay(Vector2 origin, int startAt) {
        var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(30), cell => !MapAnalyzer.IsWalkable(cell)).ToList();

        var previousIntersection = rayCastResults[0].RayIntersection;
        var previousAnimationEnd = startAt;
        foreach (var rayCastResult in rayCastResults) {
            var rayEnd = rayCastResult.RayIntersection;
            var lineDrawingAnimation = new LineDrawingAnimation(previousIntersection.ToVector3(), rayEnd.ToVector3(), Colors.Green, previousAnimationEnd)
                .WithConstantRate(4);

            AddAnimation(lineDrawingAnimation);

            var sphereDrawingAnimation = new SphereDrawingAnimation(rayEnd.ToVector3(), 0.1f, Colors.Purple, lineDrawingAnimation.EndFrame)
                .WithDurationInSeconds(0.5f);

            AddAnimation(sphereDrawingAnimation);

            previousIntersection = rayCastResult.RayIntersection;
            previousAnimationEnd = sphereDrawingAnimation.EndFrame + (int)TimeUtils.SecsToFrames(0.5f);
        }

        var cameraReadyFrame = CenterCamera(rayCastResults.First().RayIntersection, rayCastResults.Last().RayIntersection, startAt, previousAnimationEnd);

        var pauseAnimation = new PauseAnimation(cameraReadyFrame).WithDurationInSeconds(_pauseAtEndOfClipDurationSeconds);
        AddAnimation(pauseAnimation);

        CenterCamera(rayCastResults.Last().RayIntersection, rayCastResults.First().RayIntersection, pauseAnimation.EndFrame, 0.5f);
    }

    private int ShowGrid(Vector2 origin, int startAt) {
        var endFrame = startAt;

        var random = new Random();
        foreach (var cell in MapAnalyzer.BuildSearchRadius(origin, 15)) {
            var rng = (float)random.NextDouble();
            var rngStartFrame = startAt + (int)TimeUtils.SecsToFrames(rng * 1f);
            var squareAnimation = new CellDrawingAnimation(cell.ToVector3(), rngStartFrame).WithDurationInSeconds(0.5f);

            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.EndFrame);
        }

        return endFrame;
    }
}
