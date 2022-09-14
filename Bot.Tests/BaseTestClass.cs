using Bot.Tests.Fixtures;

namespace Bot.Tests;

// Collection("Sequential") makes it so each test class is part of the same collection
// This will make the tests run sequentially
// It is required because all state is global (oops) and setup/teardown might affect other tests
// This is notably caused by the Controller and its friends WhoNeedUpdating
[Collection("Sequential")]
public class BaseTestClass :
    IClassFixture<LoggerFixture>,
    IClassFixture<KnowledgeBaseFixture>,
    IClassFixture<GraphicalDebuggerFixture>,
    IClassFixture<ControllerFixture> {}
