using System.Collections.Generic;
using System.Linq;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class Clip {
    private readonly List<Animation> _animations = new List<Animation>();
    private int _clipFrame = 0;

    public bool IsDone { get; private set; } = false;

    public void AddAnimation(Animation animation) {
        _animations.Add(animation);
    }

    public void Render() {
        if (IsDone) {
            return;
        }

        foreach (var animation in _animations) {
            animation.Render(_clipFrame);
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
}
