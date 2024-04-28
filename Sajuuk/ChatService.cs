using Sajuuk.Actions;
using SC2APIProtocol;

namespace Sajuuk;

public class ChatService : IChatService {
    private readonly IActionService _actionService;

    public ChatService(IActionService actionService) {
        _actionService = actionService;
    }

    public void Chat(string message, bool toTeam = false) {
        _actionService.AddAction( new Action
        {
            ActionChat = new ActionChat
            {
                Channel = toTeam ? ActionChat.Types.Channel.Team : ActionChat.Types.Channel.Broadcast,
                Message = message
            }
        });
    }
}
