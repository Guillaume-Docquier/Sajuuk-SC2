using System.Numerics;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.Requests;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.VideoClips.Manim.Animations;

public class MoveCameraAnimation : Animation<MoveCameraAnimation> {
    private readonly IRequestBuilder _requestBuilder;
    private readonly IRequestService _requestService;

    private readonly Vector2 _origin;
    private readonly Vector2 _destination;

    public MoveCameraAnimation(
        IRequestBuilder requestBuilder,
        IRequestService requestService,
        Vector2 origin,
        Vector2 destination,
        int startFrame
    ) : base(startFrame) {
        _requestBuilder = requestBuilder;
        _requestService = requestService;

        _origin = origin;
        _destination = destination;
    }

    protected override async Task Animate(int currentClipFrame) {
        var percentDone = GetAnimationPercentDone(currentClipFrame);
        var nextPosition = Vector2.Lerp(_origin, _destination, percentDone);

        await _requestService.SendRequest(_requestBuilder.DebugMoveCamera(new Point { X = nextPosition.X, Y = nextPosition.Y, Z = 0 }));
    }

    public MoveCameraAnimation WithConstantRate(float unitsPerSecond) {
        AnimationDuration = (int)TimeUtils.SecsToFrames(_origin.DistanceTo(_destination) / unitsPerSecond);

        return this;
    }
}
