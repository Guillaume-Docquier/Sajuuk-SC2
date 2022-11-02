using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
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

    public async Task OnFrame() {
        await EnsureInitialization();

        if (_currentlyPlayingClip == null) {
            return;
        }

        if (_currentlyPlayingClip.IsDone && !_clips.TryDequeue(out _currentlyPlayingClip)) {
            return;
        }

        _currentlyPlayingClip.Render();
    }

    private async Task EnsureInitialization() {
        if (_isInitialized) {
            return;
        }

        await Program.GameConnection.SendRequest(RequestBuilder.DebugRevealMap());

        _clips.Enqueue(new RegionAnalysisClip());
        _clips.Enqueue(new RegionAnalysisClip());

        _currentlyPlayingClip = _clips.Dequeue();

        _isInitialized = true;
    }
}
