using System;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class SingleRayCastingClip : Clip {
    public SingleRayCastingClip(Vector2 origin, Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds = 5): base(pauseAtEndOfClipDurationSeconds) {
        var cameraReadyFrame = CenterCamera(origin, sceneLocation);

        var rayCast = RayCasting.RayCast(sceneLocation, MathUtils.DegToRad(30), cell => !MapAnalyzer.IsWalkable(cell)).ToList();

        var castRayReadyFrame = CastRay(rayCast.First().RayIntersection, rayCast.Last().RayIntersection, cameraReadyFrame);
        ShowWall(rayCast.Last().RayIntersection.AsWorldGridCenter(), castRayReadyFrame);
    }

    private int CastRay(Vector2 rayStart, Vector2 rayEnd, int startAt) {
        var lineDrawingAnimation = new LineDrawingAnimation(rayStart.ToVector3(), rayEnd.ToVector3(), Colors.Green, startAt)
            .WithConstantRate(3);

        AddAnimation(lineDrawingAnimation);

        return lineDrawingAnimation.EndFrame;
    }

    private void ShowWall(Vector2 wall, int startAt) {
        var squareAnimation = new CellDrawingAnimation(wall.ToVector3(), startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(squareAnimation);
    }
}
