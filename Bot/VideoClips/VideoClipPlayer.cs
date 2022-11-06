using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Bot.Builds;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Utils;
using Bot.VideoClips.Clips;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips;

public class VideoClipPlayer : IBot {
    private readonly Queue<Clip> _clips = new Queue<Clip>();
    private Clip _currentlyPlayingClip;
    private bool _isInitialized = false;

    public string Name => "VideoClipPlayer";
    public Race Race => Race.Zerg;

    private readonly BotDebugger _debugger = new BotDebugger();
    private ulong _startAt;

    public async Task OnFrame() {
        await EnsureInitialization();
        _debugger.Debug(Enumerable.Empty<BuildRequest>(), Enumerable.Empty<BuildFulfillment>());

        foreach (var unit in Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Drone).Where(unit => unit.HasOrders())) {
            unit.Stop();
        }

        if (Controller.Frame < _startAt) {
            return;
        }

        if (_currentlyPlayingClip == null) {
            Logger.Important("VideoClip done!");
            return;
        }

        if (_currentlyPlayingClip.IsDone && !_clips.TryDequeue(out _currentlyPlayingClip)) {
            return;
        }

        await _currentlyPlayingClip.Render();
    }

    private async Task EnsureInitialization() {
        if (_isInitialized) {
            return;
        }

        await Program.GameConnection.SendRequest(RequestBuilder.DebugRevealMap());
        DebuggingFlagsTracker.Instance.HandleMessage(DebuggingCommands.Off);

        _clips.Enqueue(new SingleRayCastingClip       (new Vector2(99.5f, 52.5f), pauseAtEndOfClipDurationSeconds: 5));
        _clips.Enqueue(new GridDisplayClip            (new Vector2(99.5f, 52.5f), pauseAtEndOfClipDurationSeconds: 5));
        _clips.Enqueue(new RaySteppingClip            (new Vector2(99.5f, 52.5f), pauseAtEndOfClipDurationSeconds: 5));
        _clips.Enqueue(new RayCastingIntersectionsClip(new Vector2(99.5f, 52.5f), pauseAtEndOfClipDurationSeconds: 5));
        _clips.Enqueue(new FullRayCastingClip         (new Vector2(99.5f, 52.5f), pauseAtEndOfClipDurationSeconds: 5));

        _currentlyPlayingClip = _clips.Dequeue();
        _startAt = Controller.Frame + TimeUtils.SecsToFrames(20);

        _isInitialized = true;
    }
}
