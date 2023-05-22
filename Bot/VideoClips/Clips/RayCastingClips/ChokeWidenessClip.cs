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

public class ChokeWidenessClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IAnimationFactory _animationFactory;

    public ChokeWidenessClip(
        ITerrainTracker terrainTracker,
        IAnimationFactory animationFactory,
        Vector2 origin,
        Vector2 destination,
        int pauseAtEndOfClipDurationSeconds
    ) : base(animationFactory, pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _animationFactory = animationFactory;

        var centerCameraAnimation = _animationFactory.CreateCenterCameraAnimation(origin, startFrame: 0)
            .WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var pauseAnimation = _animationFactory.CreatePauseAnimation(centerCameraAnimation.AnimationEndFrame)
            .WithDurationInSeconds(5);
        AddAnimation(pauseAnimation);

        ShowWideness(origin, destination, pauseAnimation.AnimationEndFrame);
    }

    private void ShowWideness(Vector2 origin, Vector2 destination, int startFrame) {
        var previousAnimationEndFrame = startFrame;
        for (var i = 0f; i < 0.95f; i += 0.005f) {
            var currentPosition = Vector2.Lerp(origin, destination, i);

            var left = currentPosition.TranslateTowards(destination, 1).RotateAround(currentPosition, MathUtils.DegToRad(90));
            var leftRayEnd = RayCasting.RayCast(currentPosition, left, cell => !_terrainTracker.IsWalkable(cell)).Last();
            var leftRayAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(currentPosition), _terrainTracker.WithWorldHeight(leftRayEnd.RayIntersection), Colors.DarkRed, previousAnimationEndFrame)
                .WithPostAnimationDurationInFrames(1);
            AddAnimation(leftRayAnimation);

            var right = currentPosition.TranslateTowards(destination, 1).RotateAround(currentPosition, MathUtils.DegToRad(-90));
            var rightRayEnd = RayCasting.RayCast(currentPosition, right, cell => !_terrainTracker.IsWalkable(cell)).Last();
            var rightRayAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(currentPosition), _terrainTracker.WithWorldHeight(rightRayEnd.RayIntersection), Colors.DarkRed, previousAnimationEndFrame)
                .WithPostAnimationDurationInFrames(1);
            AddAnimation(rightRayAnimation);

            previousAnimationEndFrame = rightRayAnimation.PostAnimationEndFrame;
        }

        var panCameraAnimation = _animationFactory.CreateCenterCameraAnimation(destination, startFrame)
            .WithEndFrame(previousAnimationEndFrame);
        AddAnimation(panCameraAnimation);
    }
}
