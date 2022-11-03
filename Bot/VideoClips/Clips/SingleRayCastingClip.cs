using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class SingleRayCastingClip : Clip {
    public SingleRayCastingClip() {
        var rayStart = new Vector2(99.5f, 52.5f);
        var startFrame = (int)TimeUtils.SecsToFrames(5);
        var moveCameraAnimation = new MoveCameraAnimation(rayStart, startFrame)
            .WithDurationInSeconds(5);

        var rayEnd = RayCasting.RayCast(rayStart, MathUtils.DegToRad(30), cell => !MapAnalyzer.IsWalkable(cell)).Last().RayIntersection;
        var lineDrawingAnimation1 = new LineDrawingAnimation(rayStart.ToVector3(), rayEnd.ToVector3(), Colors.Green, moveCameraAnimation.EndFrame)
            .WithConstantRate(3);

        rayEnd = RayCasting.RayCast(rayStart, MathUtils.DegToRad(90), cell => !MapAnalyzer.IsWalkable(cell)).Last().RayIntersection;
        var lineDrawingAnimation2 = new LineDrawingAnimation(rayStart.ToVector3(), rayEnd.ToVector3(), Colors.Cyan, lineDrawingAnimation1.EndFrame)
            .WithConstantRate(3);

        rayEnd = RayCasting.RayCast(rayStart, MathUtils.DegToRad(135), cell => !MapAnalyzer.IsWalkable(cell)).Last().RayIntersection;
        var lineDrawingAnimation3 = new LineDrawingAnimation(rayStart.ToVector3(), rayEnd.ToVector3(), Colors.Red, lineDrawingAnimation2.EndFrame)
            .WithConstantRate(3);

        AddAnimation(moveCameraAnimation);
        AddAnimation(lineDrawingAnimation1);
        AddAnimation(lineDrawingAnimation2);
        AddAnimation(lineDrawingAnimation3);
    }
}
