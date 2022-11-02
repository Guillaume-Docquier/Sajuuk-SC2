using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class MoveCameraAnimation : IAnimation {
    private readonly Vector2 _moveTo;
    private Vector2 _originalCameraPosition;

    public int StartFrame { get; }
    private int _duration = 0;
    public int EndFrame => StartFrame + _duration;

    public MoveCameraAnimation(Vector2 moveTo, int startFrame) {
        _moveTo = moveTo;
        StartFrame = startFrame;
    }

    public MoveCameraAnimation WithDurationInFrames(int frameDuration) {
        _duration = frameDuration;

        return this;
    }

    public MoveCameraAnimation WithDurationInSeconds(int secondsDuration) {
        _duration = (int)TimeUtils.SecsToFrames(secondsDuration);

        return this;
    }

    public MoveCameraAnimation WithEndFrame(int endFrame) {
        _duration = endFrame - StartFrame;

        return this;
    }

    public void Render(int currentClipFrame) {
        if (currentClipFrame < StartFrame || currentClipFrame > EndFrame) {
            return;
        }

        DoMoveCamera(currentClipFrame);
    }

    private void DoMoveCamera(int currentClipFrame) {
        if (currentClipFrame == StartFrame) {
            _originalCameraPosition = Controller.Observation.Observation.RawData.Player.Camera.ToVector2();
        }

        var currentDuration = currentClipFrame - StartFrame;
        var percentDone = (float)currentDuration / _duration;
        var nextPosition = Vector2.Lerp(_originalCameraPosition, _moveTo, percentDone);

        // We should support the async nature here somehow
        // Request service of make all render async
#pragma warning disable CS4014
        Program.GameConnection.SendRequest(RequestBuilder.DebugMoveCamera(new Point { X = nextPosition.X, Y = nextPosition.Y, Z = 0 }));
#pragma warning restore CS4014
    }
}
