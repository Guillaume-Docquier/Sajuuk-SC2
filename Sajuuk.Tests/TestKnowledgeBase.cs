using Sajuuk.GameData;

namespace Sajuuk.Tests;

public class TestKnowledgeBase : KnowledgeBase {
    public TestKnowledgeBase() {
        Data = KnowledgeBaseDataStore.Load();
    }
}
