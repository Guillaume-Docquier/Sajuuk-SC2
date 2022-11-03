using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Builds;
using Bot.Debugging;
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

    public async Task OnFrame() {
        await EnsureInitialization();
        _debugger.Debug(Enumerable.Empty<BuildRequest>(), Enumerable.Empty<BuildFulfillment>());

        if (_currentlyPlayingClip == null) {
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

        _clips.Enqueue(new SingleRayCastingClip());

        _currentlyPlayingClip = _clips.Dequeue();

        _isInitialized = true;
    }
}
