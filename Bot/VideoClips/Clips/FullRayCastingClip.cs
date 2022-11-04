using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class FullRayCastingClip : Clip {
    public FullRayCastingClip() {
        var origin = new Vector2(99.5f, 52.5f);
        var cameraReadyFrame = CenterCamera(origin, (int)TimeUtils.SecsToFrames(0));

        CastAllRays(origin, cameraReadyFrame);

        Pause(30);
    }

    private int CenterCamera(Vector2 origin, int startAt) {
        var moveCameraAnimation = new MoveCameraAnimation(origin, startAt)
            .WithDurationInSeconds(1);

        AddAnimation(moveCameraAnimation);

        return moveCameraAnimation.EndFrame;
    }

    private void CastAllRays(Vector2 origin, int startAt) {
        var sphereDrawingAnimation = new SphereDrawingAnimation(origin.ToVector3(), 0.5f, Colors.Purple, startAt)
            .WithDurationInSeconds(0.5f);

        AddAnimation(sphereDrawingAnimation);

        for (var angle = 0; angle < 360; angle += 2) {
            var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(angle), cell => !MapAnalyzer.IsWalkable(cell)).ToList();

            var previousIntersection = rayCastResults[0].RayIntersection;
            var previousAnimationEnd = sphereDrawingAnimation.EndFrame + (int)TimeUtils.SecsToFrames((float)angle / 360 * 2);
            foreach (var rayCastResult in rayCastResults.Skip(1)) {
                var rayEnd = rayCastResult.RayIntersection;
                var lineDrawingAnimation = new LineDrawingAnimation(previousIntersection.ToVector3(), rayEnd.ToVector3(), Colors.Green, previousAnimationEnd)
                    .WithConstantRate(4);

                AddAnimation(lineDrawingAnimation);

                var squareAnimation = new CellDrawingAnimation(rayCastResult.CornerOfCell.AsWorldGridCenter().ToVector3(), lineDrawingAnimation.EndFrame)
                    .WithDurationInSeconds(0.5f);

                AddAnimation(squareAnimation);

                previousIntersection = rayCastResult.RayIntersection;
                previousAnimationEnd = lineDrawingAnimation.EndFrame;
            }
        }
    }

    private void Pause(int durationInSeconds) {
        var bufferAnimation = new LineDrawingAnimation(new Vector3(), new Vector3(), Colors.Green, 0)
            .WithDurationInSeconds(durationInSeconds);

        AddAnimation(bufferAnimation);
    }
}
