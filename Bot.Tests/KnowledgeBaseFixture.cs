using Bot.GameData;

namespace Bot.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class KnowledgeBaseFixture {
    public KnowledgeBaseFixture() {
        KnowledgeBase.Data = KnowledgeBaseDataStore.Load();
    }
}
