using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal interface IInitializationOrchestrator
{
    KeywordSearchInfo KeywordSearchInfo { get; }
    public Task<int> InitializeAsync(int MeilisearchCount);
}
