using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Bot.Debugging.GraphicalDebugging;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public abstract class Clip {
    private readonly List<Animation> _animations = new List<Animation>();
    private int _clipFrame = 0;
    private readonly Animation _pauseAnimation;

    public bool IsDone { get; private set; } = false;

    protected Clip(int pauseAtEndOfClipDurationSeconds) {
        // Dummy animation to run at the end of the clip
        _pauseAnimation = new LineDrawingAnimation(new Vector3(), new Vector3(), Colors.Green, startFrame: 0)
            .WithDurationInSeconds(pauseAtEndOfClipDurationSeconds);

        _animations.Add(_pauseAnimation);
    }

    protected void AddAnimation(Animation animation) {
        _animations.Add(animation);

        if (animation.EndFrame > _pauseAnimation.StartFrame) {
            _pauseAnimation.ChangeStartFrame(animation.EndFrame);
        }
    }

    public async Task Render() {
        if (IsDone) {
            return;
        }

        foreach (var animation in _animations) {
            await animation.Render(_clipFrame);
        }

        _clipFrame++;

        if (_animations.All(animation => animation.EndFrame <= _clipFrame)) {
            IsDone = true;
        }
    }

    public void Reset() {
        _clipFrame = 0;
        IsDone = false;
    }

    protected int CenterCamera(Vector2 origin, Vector2 destination, int startAt = 0, int durationInSeconds = 1) {
        if (origin == destination) {
            return startAt;
        }

        var moveCameraAnimation = new MoveCameraAnimation(origin, destination, startAt)
            .WithDurationInSeconds(durationInSeconds);

        AddAnimation(moveCameraAnimation);

        return moveCameraAnimation.EndFrame;
    }
}
