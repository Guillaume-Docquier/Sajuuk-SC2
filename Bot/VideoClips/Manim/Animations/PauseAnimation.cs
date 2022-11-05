using System.Threading.Tasks;

namespace Bot.VideoClips.Manim.Animations;

public class PauseAnimation : Animation<PauseAnimation> {
    public PauseAnimation(int startFrame) : base(startFrame) {}

    protected override Task Animate(int currentClipFrame) {
        return Task.CompletedTask;
    }
}
