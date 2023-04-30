using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Bot.Debugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Utils;
using Bot.VideoClips.Clips.RayCastingClips;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.VideoClips;

public class VideoClipPlayer : IBot {
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;

    private readonly string _mapName;
    private readonly Queue<Clip> _clips = new Queue<Clip>();
    private Clip _currentlyPlayingClip;
    private bool _isInitialized = false;

    public string Name => "VideoClipPlayer";
    public Race Race => Race.Zerg;

    private ulong _startAt;

    public VideoClipPlayer(string mapName, IDebuggingFlagsTracker debuggingFlagsTracker, IUnitsTracker unitsTracker, ITerrainTracker terrainTracker) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;

        _mapName = mapName;
    }

    public async Task OnFrame() {
        await EnsureInitialization();

        foreach (var unit in Controller.GetUnits(_unitsTracker.OwnedUnits, Units.Drone).Where(unit => unit.HasOrders())) {
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
        _debuggingFlagsTracker.HandleMessage(DebuggingCommands.Off);

        ColorService.SetMap(_mapName);

        foreach (var clip in GetClipsForMap(_mapName)) {
            _clips.Enqueue(clip);
        }

        if (_clips.Any()) {
            _currentlyPlayingClip = _clips.Dequeue();
        }
        else {
            _debuggingFlagsTracker.HandleMessage(DebuggingFlags.Coordinates);
        }

        _startAt = Controller.Frame + TimeUtils.SecsToFrames(20);

        _isInitialized = true;
    }

    private IEnumerable<Clip> GetClipsForMap(string mapName) {
        switch (mapName) {
            case Maps.Season_2022_4.FileNames.Stargazers:
                yield return new PerpendicularLinesScanClip (_terrainTracker, new Vector2(50.5f,  92.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new SingleRayCastingClip       (_terrainTracker, new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (_terrainTracker, new Vector2(99.5f,  52.5f),  stepSize: 0.1f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (_terrainTracker, new Vector2(99.5f,  52.5f),  stepSize: 1.4f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new GridDisplayClip            (_terrainTracker, new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new StepComparisonClip         (_terrainTracker, new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RaySteppingClip            (_terrainTracker, new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RayCastingIntersectionsClip(_terrainTracker, new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(99.5f,  52.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(111.5f, 33.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(148.5f, 91.5f),  pauseAtEndOfClipDurationSeconds: 5);
                break;
            case Maps.Season_2022_4.FileNames.CosmicSapphire:
                yield return new SingleRayCastingClip       (_terrainTracker, new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (_terrainTracker, new Vector2(132.5f, 47.5f),  stepSize: 0.1f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (_terrainTracker, new Vector2(132.5f, 47.5f),  stepSize: 1.4f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new GridDisplayClip            (_terrainTracker, new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RaySteppingClip            (_terrainTracker, new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new RayCastingIntersectionsClip(_terrainTracker, new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new ChokeWidenessClip          (_terrainTracker, new Vector2(27.5f, 100.5f),  new Vector2(38.5f, 89.5f), pauseAtEndOfClipDurationSeconds: 5);
                yield return new ChokeWallsClip             (_terrainTracker, new Vector2(32.5f, 94.5f),   pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(132.5f, 47.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(134.5f, 133.5f), pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(32.5f,  94.5f),  pauseAtEndOfClipDurationSeconds: 5);
                break;
            case Maps.Season_2022_4.FileNames.Hardwire:
                yield return new PerpendicularLinesScanClip (_terrainTracker, new Vector2(126.5f, 158.5f),  pauseAtEndOfClipDurationSeconds: 5);
                yield return new SingleRayCastingClip       (_terrainTracker, new Vector2(80.5f,  82.5f),   pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (_terrainTracker, new Vector2(80.5f,  82.5f),   stepSize: 0.1f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new NaiveRayCastClip           (_terrainTracker, new Vector2(80.5f,  82.5f),   stepSize: 1.4f, pauseAtEndOfClipDurationSeconds: 5);
                yield return new GridDisplayClip            (_terrainTracker, new Vector2(80.5f,  82.5f),   pauseAtEndOfClipDurationSeconds: 5);
                yield return new RaySteppingClip            (_terrainTracker, new Vector2(80.5f,  82.5f),   pauseAtEndOfClipDurationSeconds: 5);
                yield return new RayCastingIntersectionsClip(_terrainTracker, new Vector2(80.5f,  82.5f),   pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(80.5f,  82.5f),   pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(100.5f, 60.5f),   pauseAtEndOfClipDurationSeconds: 5);
                yield return new FullRayCastingClip         (_terrainTracker, new Vector2(126.5f, 65.5f),   pauseAtEndOfClipDurationSeconds: 5);
                break;
        }
    }
}
