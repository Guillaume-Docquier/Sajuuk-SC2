using System.Collections.Generic;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class Clip {
    private readonly List<Animation> _animations = new List<Animation>();
    private int _clipFrame = 0;

    public void AddAnimation(Animation animation) {
        _animations.Add(animation);
    }

    public void Render() {
        foreach (var animation in _animations) {
            animation.Render(_clipFrame);
        }

        _clipFrame += 2; // TODO GD Put the game in single frame mode
    }

    public void Reset() {
        _clipFrame = 0;
    }
}
