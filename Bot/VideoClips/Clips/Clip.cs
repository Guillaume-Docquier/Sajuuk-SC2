using System.Collections.Generic;
using Bot.VideoClips.Manim.Animations;
using Bot.Wrapper;

namespace Bot.VideoClips.Clips;

public class Clip {
    private readonly List<Animation> _animations = new List<Animation>();
    private int _clipFrame = 0;

    public void AddAnimation(Animation animation) {
        _animations.Add(animation);
    }

    public void Render() {
        if (_clipFrame == 0) {
            // Really need to make this stuff async and have better scaffolding
#pragma warning disable CS4014
            Program.GameConnection.SendRequest(RequestBuilder.DebugRevealMap());
#pragma warning restore CS4014
        }

        foreach (var animation in _animations) {
            animation.Render(_clipFrame);
        }

        _clipFrame += 2; // TODO GD Put the game in single frame mode
    }

    public void Reset() {
        _clipFrame = 0;
    }
}
