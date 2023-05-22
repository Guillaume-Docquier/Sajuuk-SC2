using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class SingleRayCastingClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IAnimationFactory _animationFactory;

    public SingleRayCastingClip(
        ITerrainTracker terrainTracker,
        IAnimationFactory animationFactory,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds = 5
    ) : base(animationFactory, pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _animationFactory = animationFactory;

        var centerCameraAnimation = _animationFactory.CreateCenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var pauseAnimation = _animationFactory.CreatePauseAnimation(centerCameraAnimation.AnimationEndFrame).WithDurationInSeconds(1);
        AddAnimation(pauseAnimation);

        var rayCast = RayCasting.RayCast(sceneLocation, MathUtils.DegToRad(30), cell => !_terrainTracker.IsWalkable(cell)).ToList();

        var castRayReadyFrame = CastRay(rayCast.First().RayIntersection, rayCast.Last().RayIntersection, pauseAnimation.AnimationEndFrame);
        ShowWall(rayCast.Last().RayIntersection.AsWorldGridCenter(), castRayReadyFrame);
    }

    private int CastRay(Vector2 rayStart, Vector2 rayEnd, int startAt) {
        var showPointReadyFrame = ShowPoint(rayStart, startAt);

        var pauseAnimation = _animationFactory.CreatePauseAnimation(showPointReadyFrame).WithDurationInSeconds(1);
        AddAnimation(pauseAnimation);

        var lineDrawingAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(rayStart), _terrainTracker.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, pauseAnimation.AnimationEndFrame)
            .WithConstantRate(3);

        AddAnimation(lineDrawingAnimation);

        var centerCameraAnimation = _animationFactory.CreateCenterCameraAnimation(rayEnd, lineDrawingAnimation.StartFrame).WithEndFrame(lineDrawingAnimation.AnimationEndFrame);
        AddAnimation(centerCameraAnimation);

        ShowPoint(rayEnd, lineDrawingAnimation.AnimationEndFrame);

        return lineDrawingAnimation.AnimationEndFrame;
    }

    private void ShowWall(Vector2 wall, int startAt) {
        var squareAnimation = _animationFactory.CreateCellDrawingAnimation(_terrainTracker.WithWorldHeight(wall), startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(squareAnimation);
    }

    private int ShowPoint(Vector2 origin, int startAt) {
        var sphereAnimation = _animationFactory.CreateSphereDrawingAnimation(_terrainTracker.WithWorldHeight(origin), radius: 0.15f, ColorService.Instance.PointColor, startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(sphereAnimation);

        return sphereAnimation.AnimationEndFrame;
    }
}
