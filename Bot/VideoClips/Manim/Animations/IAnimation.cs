namespace Bot.VideoClips.Manim.Animations;

public interface IAnimation {
    public int StartFrame { get; }
    public int EndFrame { get; }

    public void Render(int currentClipFrame);
}
