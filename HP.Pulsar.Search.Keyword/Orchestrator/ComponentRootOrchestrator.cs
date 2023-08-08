using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ComponentRootOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo _keywordSearchInfo { get; }

    public ComponentRootOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        _keywordSearchInfo = keywordSearchInfo;
    }

    public async Task InitializeAsync()
    {
        // read roots from database
        ComponentRootReader reader = new(_keywordSearchInfo);
        IEnumerable<CommonDataModel> roots = await reader.GetDataAsync();

        // data processing
        ComponentRootTransformer tranformer = new();
        roots = tranformer.Transform(roots);

        // summary property
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(roots.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.ComponentRoot);
        await writer.InitializeIndexCreationStepsAsync(roots, elementKeyContainer.Get(), 10000);
    }
}
