namespace Bot.UnitModules;

public interface IUnitModule {
    void Enable();

    void Disable();

    bool Execute();
}
