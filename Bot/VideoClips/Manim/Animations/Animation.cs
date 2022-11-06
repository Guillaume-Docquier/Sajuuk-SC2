using System.Threading.Tasks;
using Bot.Utils;

namespace Bot.VideoClips.Manim.Animations;

public abstract class Animation {
    private const int PostAnimateForever = -1;

    public int StartFrame { get; private set; }

    public int AnimationDuration { get; protected set; } = 0;
    public int AnimationEndFrame => StartFrame + AnimationDuration;

    public int PostAnimationDuration { get; protected set; } = PostAnimateForever;
    public int PostAnimationEndFrame => AnimationEndFrame + PostAnimationDuration;

    protected Animation(int startFrame) {
        StartFrame = startFrame;
    }

    public async Task Render(int currentClipFrame) {
        if (currentClipFrame < StartFrame) {
            PreAnimate(currentClipFrame);
        }
        else if (currentClipFrame <= AnimationEndFrame) {
            await Animate(currentClipFrame);
        }
        else if (PostAnimationDuration == PostAnimateForever || currentClipFrame <= PostAnimationEndFrame) {
            PostAnimate(currentClipFrame);
        }
    }

    public void ChangeStartFrame(int newStartFrame) {
        StartFrame = newStartFrame;
    }

    protected float GetAnimationPercentDone(int currentClipFrame) {
        if (currentClipFrame < StartFrame) {
            return 0;
        }

        if (currentClipFrame > AnimationEndFrame) {
            return 1;
        }

        var currentDuration = currentClipFrame - StartFrame;

        return (float)currentDuration / AnimationDuration;
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
    protected Animation(int startFrame) : base(startFrame) {}

    /// <summary>
    /// Builder method to set the animation duration in terms of frames.
    /// Returns the derived instance so that it can be chained.
    /// </summary>
    /// <param name="frameDuration">The amount of frames the animation should last</param>
    /// <returns>The derived instance so that it can be chained</returns>
    public TAnimation WithDurationInFrames(int frameDuration) {
        AnimationDuration = frameDuration;

        return (TAnimation)this;
    }

    /// <summary>
    /// Builder method to set the animation duration in terms of seconds.
    /// </summary>
    /// <param name="secondsDuration">The amount of seconds the animation should last</param>
    /// <returns>The derived instance so that it can be chained</returns>
    public TAnimation WithDurationInSeconds(float secondsDuration) {
        AnimationDuration = (int)TimeUtils.SecsToFrames(secondsDuration);

        return (TAnimation)this;
    }

    /// <summary>
    /// Builder method to set the animation duration based on the end frame.
    /// Returns the derived instance so that it can be chained.
    /// </summary>
    /// <param name="endFrame">The frame at which the animation should end</param>
    /// <returns>The derived instance so that it can be chained</returns>
    public TAnimation WithEndFrame(int endFrame) {
        AnimationDuration = endFrame - StartFrame;

        return (TAnimation)this;
    }

    public TAnimation WithPostAnimationDurationInSeconds(int secondsDuration) {
        PostAnimationDuration = (int)TimeUtils.SecsToFrames(secondsDuration);

        return (TAnimation)this;
    }

    public TAnimation WithPostAnimationEndFrame(int endFrame) {
        PostAnimationDuration = endFrame - AnimationEndFrame;

        return (TAnimation)this;
    }
}
