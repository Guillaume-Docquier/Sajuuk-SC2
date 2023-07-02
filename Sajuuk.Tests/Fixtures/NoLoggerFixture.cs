namespace Sajuuk.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class NoLoggerFixture {
    public NoLoggerFixture() {
        Logger.Disable();
    }
}
