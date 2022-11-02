using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.VideoClips.Manim;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class RegionAnalysisClip : Clip {
    public RegionAnalysisClip() {
        var startFrame = TimeUtils.SecsToFrames(5);

        var clipLocation = new Vector2(99.5f, 49.5f);
        var moveCameraAnimation = new MoveCameraAnimation(clipLocation, (int)startFrame)
            .WithDurationInSeconds(5);

        var lineStart = clipLocation;
        var lineEnd = lineStart.Translate(xTranslation: 5, yTranslation: 5);
        var lineDrawingAnimation = new LineDrawingAnimation(lineStart.ToVector3(), lineEnd.ToVector3(), Colors.Cyan, moveCameraAnimation.EndFrame)
            .WithDurationInSeconds(1);

        AddAnimation(moveCameraAnimation); // 5 - 10
        AddAnimation(lineDrawingAnimation); // 10 - 11
    }
}
