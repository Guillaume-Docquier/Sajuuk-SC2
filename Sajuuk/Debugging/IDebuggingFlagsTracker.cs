namespace Sajuuk.Debugging;

public interface IDebuggingFlagsTracker {
    public bool IsActive(string debuggingFlag);
    public void HandleMessage(string message);
}
