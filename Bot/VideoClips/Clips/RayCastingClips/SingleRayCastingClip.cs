using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Requests;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class SingleRayCastingClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IController _controller;
    private readonly IRequestBuilder _requestBuilder;
    private readonly IRequestService _requestService;

    public SingleRayCastingClip(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IController controller,
        IRequestBuilder requestBuilder,
        IRequestService requestService,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds = 5
    ) : base(pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _controller = controller;
        _requestBuilder = requestBuilder;
        _requestService = requestService;

        var centerCameraAnimation = new CenterCameraAnimation(_controller, _requestBuilder, _requestService, sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var pauseAnimation = new PauseAnimation(centerCameraAnimation.AnimationEndFrame).WithDurationInSeconds(1);
        AddAnimation(pauseAnimation);

        var rayCast = RayCasting.RayCast(sceneLocation, MathUtils.DegToRad(30), cell => !_terrainTracker.IsWalkable(cell)).ToList();

        var castRayReadyFrame = CastRay(rayCast.First().RayIntersection, rayCast.Last().RayIntersection, pauseAnimation.AnimationEndFrame);
        ShowWall(rayCast.Last().RayIntersection.AsWorldGridCenter(), castRayReadyFrame);
    }

    private int CastRay(Vector2 rayStart, Vector2 rayEnd, int startAt) {
        var showPointReadyFrame = ShowPoint(rayStart, startAt);

        var pauseAnimation = new PauseAnimation(showPointReadyFrame).WithDurationInSeconds(1);
        AddAnimation(pauseAnimation);

        var lineDrawingAnimation = new LineDrawingAnimation(_graphicalDebugger, _terrainTracker.WithWorldHeight(rayStart), _terrainTracker.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, pauseAnimation.AnimationEndFrame)
            .WithConstantRate(3);

        AddAnimation(lineDrawingAnimation);

        var centerCameraAnimation = new CenterCameraAnimation(_controller, _requestBuilder, _requestService, rayEnd, lineDrawingAnimation.StartFrame).WithEndFrame(lineDrawingAnimation.AnimationEndFrame);
        AddAnimation(centerCameraAnimation);

        ShowPoint(rayEnd, lineDrawingAnimation.AnimationEndFrame);

        return lineDrawingAnimation.AnimationEndFrame;
    }

    private void ShowWall(Vector2 wall, int startAt) {
        var squareAnimation = new CellDrawingAnimation(_terrainTracker, _graphicalDebugger, _terrainTracker.WithWorldHeight(wall), startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(squareAnimation);
    }

    private int ShowPoint(Vector2 origin, int startAt) {
        var sphereAnimation = new SphereDrawingAnimation(_graphicalDebugger, _terrainTracker.WithWorldHeight(origin), radius: 0.15f, ColorService.Instance.PointColor, startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(sphereAnimation);

        return sphereAnimation.AnimationEndFrame;
    }
}
