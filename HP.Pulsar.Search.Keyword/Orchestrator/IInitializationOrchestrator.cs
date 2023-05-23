using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal interface IInitializationOrchestrator
{
    KeywordSearchInfo KeywordSearchInfo { get; }
    public Task InitializeAsync();
}
