namespace Bot.Managers.ArmyManagement;

public interface IStrategy {
    string Name { get; }

    bool CanTransition();

    IStrategy Transition();

    void Execute();
}
