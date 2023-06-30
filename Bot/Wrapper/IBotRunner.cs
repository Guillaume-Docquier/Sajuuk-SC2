using System.Threading.Tasks;

namespace Bot.Wrapper;

public interface IBotRunner {
    public Task RunBot(IBot bot);
}
