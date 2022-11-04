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
    public SingleRayCastingClip(Vector2 origin) {
        var cameraReadyFrame = CenterCamera(origin, (int)TimeUtils.SecsToFrames(0));
        ShowGrid(origin, cameraReadyFrame);
        CastRay(origin, cameraReadyFrame);

        Pause(60);
    }

    private int CenterCamera(Vector2 origin, int startAt) {
        var moveCameraAnimation = new MoveCameraAnimation(origin, startAt)
            .WithDurationInSeconds(2);

        AddAnimation(moveCameraAnimation);

        return moveCameraAnimation.EndFrame;
    }

    private void ShowGrid(Vector2 origin, int startAt) {
        var random = new Random();
        foreach (var cell in MapAnalyzer.BuildSearchRadius(origin, 15)) {
            var rng = (float)random.NextDouble();
            var rngStartFrame = startAt + (int)TimeUtils.SecsToFrames(rng * 2f);
            var squareAnimation = new CellDrawingAnimation(cell.ToVector3(), rngStartFrame).WithDurationInSeconds(0.5f);
            AddAnimation(squareAnimation);
        }
    }

    private void CastRay(Vector2 origin, int startAt) {
        var rayCast = RayCasting.RayCast(origin, MathUtils.DegToRad(30), cell => !MapAnalyzer.IsWalkable(cell)).ToList();

        var rayEnd = rayCast.Last().RayIntersection;
        var lineDrawingAnimation = new LineDrawingAnimation(origin.ToVector3(), rayEnd.ToVector3(), Colors.Green, startAt)
            .WithConstantRate(3);

        AddAnimation(lineDrawingAnimation);
    }

    private void Pause(int durationInSeconds) {
        var bufferAnimation = new LineDrawingAnimation(new Vector3(), new Vector3(), Colors.Green, 0)
            .WithDurationInSeconds(durationInSeconds);

        AddAnimation(bufferAnimation);
    }
}
