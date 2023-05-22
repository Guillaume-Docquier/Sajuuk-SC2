using System;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;
using Bot.Wrapper;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class RayCastingIntersectionsClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IController _controller;
    private readonly IRequestBuilder _requestBuilder;

    public RayCastingIntersectionsClip(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IController controller,
        IRequestBuilder requestBuilder,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds = 5
    ) : base(pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _controller = controller;
        _requestBuilder = requestBuilder;

        var centerCameraAnimation = new CenterCameraAnimation(_controller, _requestBuilder, sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var gridReadyFrame = ShowGrid(sceneLocation, centerCameraAnimation.AnimationEndFrame);
        CastRay(sceneLocation, gridReadyFrame);
    }

    private int ShowGrid(Vector2 origin, int startAt) {
        var endFrame = startAt;

        var random = new Random();
        foreach (var cell in _terrainTracker.BuildSearchRadius(origin, 15)) {
            var rng = (float)random.NextDouble();
            var rngStartFrame = startAt + (int)TimeUtils.SecsToFrames(rng * 1f);
            var squareAnimation = new CellDrawingAnimation(_terrainTracker, _graphicalDebugger, _terrainTracker.WithWorldHeight(cell), rngStartFrame).WithDurationInSeconds(0.5f);

            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }

    private void CastRay(Vector2 origin, int startAt) {
        var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(30), cell => !_terrainTracker.IsWalkable(cell)).ToList();

        var previousIntersection = rayCastResults[0].RayIntersection;
        var previousAnimationEnd = startAt;
        foreach (var rayCastResult in rayCastResults) {
            var rayEnd = rayCastResult.RayIntersection;
            var lineDrawingAnimation = new LineDrawingAnimation(_graphicalDebugger, _terrainTracker.WithWorldHeight(previousIntersection), _terrainTracker.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, previousAnimationEnd)
                .WithConstantRate(4);

            AddAnimation(lineDrawingAnimation);

            var sphereDrawingAnimation = new SphereDrawingAnimation(_graphicalDebugger, _terrainTracker.WithWorldHeight(rayEnd), 0.1f, ColorService.Instance.PointColor, lineDrawingAnimation.AnimationEndFrame)
                .WithDurationInSeconds(0.5f);

            AddAnimation(sphereDrawingAnimation);

            previousIntersection = rayCastResult.RayIntersection;
            previousAnimationEnd = sphereDrawingAnimation.AnimationEndFrame + (int)TimeUtils.SecsToFrames(0.5f);
        }

        var cameraMoveToEndAnimation = new CenterCameraAnimation(_controller, _requestBuilder, rayCastResults.Last().RayIntersection, startAt)
            .WithEndFrame(previousAnimationEnd);

        AddAnimation(cameraMoveToEndAnimation);
    }
}
