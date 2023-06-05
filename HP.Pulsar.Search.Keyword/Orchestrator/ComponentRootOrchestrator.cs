using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ComponentRootOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }
    private MeiliSearchClient MeiliSearchClient { get; }

    public ComponentRootOrchestrator(KeywordSearchInfo keywordSearchInfo, MeiliSearchClient meiliSearchClient)
    {
        KeywordSearchInfo = keywordSearchInfo;
        MeiliSearchClient = meiliSearchClient;
    }

    public async Task InitializeAsync()
    {
        // read roots from database
        ComponentRootReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> roots = await reader.GetDataAsync();

        // data processing
        ComponentRootTransformer tranformer = new();
        roots = tranformer.Transform(roots);

        // summary property
        ElementKeyContainer.Add(roots.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        await MeiliSearchClient.SendElementsCreationAsync(roots);
    }
}
