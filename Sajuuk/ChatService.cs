using Sajuuk.Actions;

namespace Sajuuk;

public class ChatService : IChatService {
    private readonly IActionService _actionService;
    private readonly IActionBuilder _actionBuilder;

    public ChatService(IActionService actionService, IActionBuilder actionBuilder) {
        _actionService = actionService;
        _actionBuilder = actionBuilder;
    }

    public void Chat(string message, bool toTeam = false) {
        _actionService.AddAction(_actionBuilder.Chat(message, toTeam));
    }
}
