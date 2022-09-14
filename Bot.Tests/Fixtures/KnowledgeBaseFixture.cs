using Bot.GameData;

namespace Bot.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class KnowledgeBaseFixture {
    public KnowledgeBaseFixture() {
        KnowledgeBase.Data = KnowledgeBaseDataStore.Load();
    }
}
