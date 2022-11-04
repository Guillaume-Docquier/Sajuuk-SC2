using System.Threading.Tasks;
using Bot.Utils;

namespace Bot.VideoClips.Manim.Animations;

public abstract class Animation {
    public int StartFrame { get; }
    public abstract int Duration { get; protected set; }
    public int EndFrame => StartFrame + Duration;

    protected Animation(int startFrame) {
        StartFrame = startFrame;
    }

    public async Task Render(int currentClipFrame) {
        if (currentClipFrame < StartFrame) {
            PreAnimate(currentClipFrame);
        }
        else if (currentClipFrame <= EndFrame) {
            await Animate(currentClipFrame);
        }
        else {
            PostAnimate(currentClipFrame);
        }
    }

    protected float GetAnimationPercentDone(int currentClipFrame) {
        if (currentClipFrame < StartFrame) {
            return 0;
        }

        if (currentClipFrame > EndFrame) {
            return 1;
        }

        var currentDuration = currentClipFrame - StartFrame;

        return (float)currentDuration / Duration;
    }

    protected virtual void PreAnimate(int currentClipFrame) {
        return;
    }

    protected abstract Task Animate(int currentClipFrame);

    protected virtual void PostAnimate(int currentClipFrame) {
        return;
    }
}

public abstract class Animation<TAnimation> : Animation where TAnimation : Animation<TAnimation> {
    public override int Duration { get; protected set; } = 0;

    protected Animation(int startFrame) : base(startFrame) {}

    /// <summary>
    /// Builder method to set the animation duration in terms of frames.
    /// Returns the derived instance so that it can be chained.
    /// </summary>
    /// <param name="frameDuration">The amount of frames the animation should last</param>
    /// <returns>The derived instance so that it can be chained</returns>
    public TAnimation WithDurationInFrames(int frameDuration) {
        Duration = frameDuration;

        return (TAnimation)this;
    }

    /// <summary>
    /// Builder method to set the animation duration in terms of seconds.
    /// </summary>
    /// <param name="secondsDuration">The amount of seconds the animation should last</param>
    /// <returns>The derived instance so that it can be chained</returns>
    public TAnimation WithDurationInSeconds(float secondsDuration) {
        Duration = (int)TimeUtils.SecsToFrames(secondsDuration);

        return (TAnimation)this;
    }

    /// <summary>
    /// Builder method to set the animation duration based on the end frame.
    /// Returns the derived instance so that it can be chained.
    /// </summary>
    /// <param name="endFrame">The frame at which the animation should end</param>
    /// <returns>The derived instance so that it can be chained</returns>
    public TAnimation WithEndFrame(int endFrame) {
        Duration = endFrame - StartFrame;

        return (TAnimation)this;
    }
}
