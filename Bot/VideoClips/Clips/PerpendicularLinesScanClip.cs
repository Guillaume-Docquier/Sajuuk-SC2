using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class PerpendicularLinesScanClip : Clip {
    public PerpendicularLinesScanClip(Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds) : base(pauseAtEndOfClipDurationSeconds) {
        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0)
            .WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var pauseAnimation = new PauseAnimation(centerCameraAnimation.AnimationEndFrame)
            .WithDurationInSeconds(2);
        AddAnimation(pauseAnimation);

        ScanTerrainWithLines(sceneLocation, pauseAnimation.AnimationEndFrame);
    }

    private void ScanTerrainWithLines(Vector2 location, int startFrame) {
        var previousAnimationEndFrame = startFrame;
        for (var angle = 0; angle <= 90; angle += 1) {
            previousAnimationEndFrame = DrawCross(location, angle, previousAnimationEndFrame);
        }
    }

    private int DrawCross(Vector2 origin, double angle, int startFrame) {
        DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle), cell => !MapAnalyzer.IsWalkable(cell)).ToList(), startFrame);
        DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle + 90), cell => !MapAnalyzer.IsWalkable(cell)).ToList(), startFrame);
        DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle + 180), cell => !MapAnalyzer.IsWalkable(cell)).ToList(), startFrame);

        return DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle + 270), cell => !MapAnalyzer.IsWalkable(cell)).ToList(), startFrame);
    }

    private int DrawRaySegments(List<RayCasting.RayCastResult> rayCastResults, int startFrame) {
        var previousRayEnd = rayCastResults[0].RayIntersection;
        foreach (var rayCastResult in rayCastResults) {
            DrawRay(previousRayEnd, rayCastResult.RayIntersection, startFrame);
            previousRayEnd = rayCastResult.RayIntersection;
        }

        return startFrame + 1; // Yeah I'm lazy at this point
    }

    private int DrawRay(Vector2 origin, Vector2 destination, int startFrame) {
        var lineDrawingAnimation = new LineDrawingAnimation(origin.ToVector3(), destination.ToVector3(), Colors.DarkRed, startFrame)
            .WithPostAnimationDurationInFrames(1);

        AddAnimation(lineDrawingAnimation);

        return lineDrawingAnimation.PostAnimationEndFrame;
    }
}
