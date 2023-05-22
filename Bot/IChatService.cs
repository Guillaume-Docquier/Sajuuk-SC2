namespace Bot;

public interface IChatService {
    public void Chat(string message, bool toTeam = false);
}
