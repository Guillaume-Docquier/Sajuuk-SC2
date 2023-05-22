using System.Numerics;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class CenterCameraAnimation : Animation<CenterCameraAnimation> {
    private const int CenteringCorrectionOffset = -2;

    private readonly IController _controller;
    private readonly IRequestBuilder _requestBuilder;

    private Vector2 _origin;
    private readonly Vector2 _destination;

    public CenterCameraAnimation(
        IController controller,
        IRequestBuilder requestBuilder,
        Vector2 destination,
        int startFrame
    ) : base(startFrame) {
        _controller = controller;
        _requestBuilder = requestBuilder;

        _destination = destination.Translate(yTranslation: CenteringCorrectionOffset);
    }

    protected override async Task Animate(int currentClipFrame) {
        if (currentClipFrame == StartFrame) {
            _origin = _controller.GetCurrentCameraLocation().ToVector2();
        }

        var percentDone = GetAnimationPercentDone(currentClipFrame);
        var nextPosition = Vector2.Lerp(_origin, _destination, percentDone);

        await Program.GameConnection.SendRequest(_requestBuilder.DebugMoveCamera(new Point { X = nextPosition.X, Y = nextPosition.Y }));
    }
}
