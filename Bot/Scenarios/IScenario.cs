using System.Threading.Tasks;

namespace Bot.Scenarios;

public interface IScenario {
    Task OnFrame();
}
