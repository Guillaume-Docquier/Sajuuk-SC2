namespace Bot;

public interface IStrategy {
    bool CanTransition();

    IStrategy Transition();

    void Execute();
}
