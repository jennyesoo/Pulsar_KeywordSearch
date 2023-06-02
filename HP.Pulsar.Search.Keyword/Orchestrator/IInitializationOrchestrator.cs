using HP.Pulsar.Search.Keyword.Infrastructure;
using HP.Pulsar.Search.Keyword.SearchEngine;
namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal interface IInitializationOrchestrator
{
    private static KeywordSearchInfo KeywordSearchInfo { get; }
    private static MeiliSearchClient MeiliSearchClient { get; }
    public Task InitializeAsync();
}
