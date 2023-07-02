using System.Threading.Tasks;

namespace Sajuuk.Scenarios;

public interface IScenario {
    Task OnFrame();
}
