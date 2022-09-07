namespace Bot.Tests;

public class BaseTestClass:
    IClassFixture<LoggerFixture>,
    IClassFixture<KnowledgeBaseFixture>,
    IClassFixture<GraphicalDebuggerFixture> {}
