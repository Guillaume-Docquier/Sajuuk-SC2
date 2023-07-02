using System.Numerics;
using System.Threading.Tasks;
using Sajuuk.ExtensionMethods;
using Sajuuk.Utils;
using Sajuuk.Wrapper;
using SC2APIProtocol;

namespace Sajuuk.VideoClips.Manim.Animations;

public class MoveCameraAnimation : Animation<MoveCameraAnimation> {
    private readonly IRequestBuilder _requestBuilder;
    private readonly ISc2Client _sc2Client;

    private readonly Vector2 _origin;
    private readonly Vector2 _destination;

    public MoveCameraAnimation(
        IRequestBuilder requestBuilder,
        ISc2Client sc2Client,
        Vector2 origin,
        Vector2 destination,
        int startFrame
    ) : base(startFrame) {
        _requestBuilder = requestBuilder;
        _sc2Client = sc2Client;

        _origin = origin;
        _destination = destination;
    }

    protected override async Task Animate(int currentClipFrame) {
        var percentDone = GetAnimationPercentDone(currentClipFrame);
        var nextPosition = Vector2.Lerp(_origin, _destination, percentDone);

        await _sc2Client.SendRequest(_requestBuilder.DebugMoveCamera(new Point { X = nextPosition.X, Y = nextPosition.Y, Z = 0 }));
    }

    public MoveCameraAnimation WithConstantRate(float unitsPerSecond) {
        AnimationDuration = (int)TimeUtils.SecsToFrames(_origin.DistanceTo(_destination) / unitsPerSecond);

        return this;
    }
}
