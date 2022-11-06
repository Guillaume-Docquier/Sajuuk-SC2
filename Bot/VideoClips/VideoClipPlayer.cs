using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Bot.Builds;
using Bot.Debugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Utils;
using Bot.VideoClips.Clips;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips;

public class VideoClipPlayer : IBot {
    private readonly string _mapName;
    private readonly Queue<Clip> _clips = new Queue<Clip>();
    private Clip _currentlyPlayingClip;
    private bool _isInitialized = false;

    public string Name => "VideoClipPlayer";
    public Race Race => Race.Zerg;

    private readonly BotDebugger _debugger = new BotDebugger();
    private ulong _startAt;

    public VideoClipPlayer(string mapName) {
        _mapName = mapName;
    }

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

        ColorService.SetMap(_mapName);

        foreach (var clip in GetClipsForMap(_mapName)) {
            _clips.Enqueue(clip);
        }

        if (_clips.Any()) {
            _currentlyPlayingClip = _clips.Dequeue();
        }
        else {
            DebuggingFlagsTracker.Instance.HandleMessage(DebuggingFlags.Coordinates);
        }

        _startAt = Controller.Frame + TimeUtils.SecsToFrames(20);

        _isInitialized = true;
    }

    private static IEnumerable<Clip> GetClipsForMap(string mapName) {
        switch (mapName) {
            case Maps.Season_2022_4.FileNames.Stargazers:
                yield return new SingleRayCastingClip       (new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (new Vector2(99.5f,  52.5f),  stepSize: 0.1f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (new Vector2(99.5f,  52.5f),  stepSize: 1.4f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new GridDisplayClip            (new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RaySteppingClip            (new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RayCastingIntersectionsClip(new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(111.5f, 33.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(148.5f, 91.5f),  pauseAtEndOfClipDurationSeconds: 5);
                break;
            case Maps.Season_2022_4.FileNames.CosmicSapphire:
                yield return new SingleRayCastingClip       (new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (new Vector2(132.5f, 47.5f),  stepSize: 0.1f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (new Vector2(132.5f, 47.5f),  stepSize: 1.4f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new GridDisplayClip            (new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RaySteppingClip            (new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RayCastingIntersectionsClip(new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(134.5f, 133.5f), pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(32.5f,  94.5f),  pauseAtEndOfClipDurationSeconds: 5);
                break;
            case Maps.Season_2022_4.FileNames.Hardwire:
                yield return new SingleRayCastingClip       (new Vector2(80.5f,  82.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (new Vector2(80.5f,  82.5f),  stepSize: 0.1f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (new Vector2(80.5f,  82.5f),  stepSize: 1.4f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new GridDisplayClip            (new Vector2(80.5f,  82.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RaySteppingClip            (new Vector2(80.5f,  82.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RayCastingIntersectionsClip(new Vector2(80.5f,  82.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(80.5f,  82.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(100.5f, 60.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (new Vector2(126.5f, 65.5f),  pauseAtEndOfClipDurationSeconds: 5);
                break;
        }
    }
}
