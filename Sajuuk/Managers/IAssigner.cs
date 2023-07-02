namespace Sajuuk.Managers;

/// <summary>
/// An Assigner performs manager specific initialization on assigned units such as installing modules and tracking assigned units
/// </summary>
public interface IAssigner {
    void Assign(Unit unit);
}
