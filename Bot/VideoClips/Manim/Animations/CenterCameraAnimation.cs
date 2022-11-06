using System.Numerics;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class CenterCameraAnimation : Animation<CenterCameraAnimation> {
    private const int CenteringCorrectionOffset = -2;

    private Vector2 _origin;
    private readonly Vector2 _destination;

    public CenterCameraAnimation(Vector2 destination, int startFrame): base(startFrame) {
        _destination = destination.Translate(yTranslation: CenteringCorrectionOffset);
    }

    protected override async Task Animate(int currentClipFrame) {
        if (currentClipFrame == StartFrame) {
            _origin = Controller.GetCurrentCameraLocation().ToVector2();
        }

        var percentDone = GetAnimationPercentDone(currentClipFrame);
        var nextPosition = Vector2.Lerp(_origin, _destination, percentDone);

        await Program.GameConnection.SendRequest(RequestBuilder.DebugMoveCamera(new Point { X = nextPosition.X, Y = nextPosition.Y }));
    }
}
