namespace Bot.Managers;

/// <summary>
/// An Assigner performs manager specific clean up on released units such as uninstalling modules and untracking released units
/// </summary>
public interface IReleaser {
    void Release(Unit unit);
}
