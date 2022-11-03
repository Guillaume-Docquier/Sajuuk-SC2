using System.Numerics;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class MoveCameraAnimation : Animation<MoveCameraAnimation> {
    private readonly Vector2 _moveTo;
    private Vector2 _originalCameraPosition;

    public MoveCameraAnimation(Vector2 moveTo, int startFrame): base(startFrame) {
        _moveTo = moveTo;
    }

    protected override async Task Animate(int currentClipFrame) {
        if (currentClipFrame == StartFrame) {
            _originalCameraPosition = Controller.Observation.Observation.RawData.Player.Camera.ToVector2();
        }

        var currentDuration = currentClipFrame - StartFrame;
        var percentDone = (float)currentDuration / Duration;
        var nextPosition = Vector2.Lerp(_originalCameraPosition, _moveTo, percentDone);

        // We should support the async nature here somehow
        // Request service of make all render async
        await Program.GameConnection.SendRequest(RequestBuilder.DebugMoveCamera(new Point { X = nextPosition.X, Y = nextPosition.Y, Z = 0 }));
    }
}
