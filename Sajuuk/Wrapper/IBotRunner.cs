using System.Threading.Tasks;

namespace Sajuuk.Wrapper;

public interface IBotRunner {
    public Task RunBot(IBot bot);
}
