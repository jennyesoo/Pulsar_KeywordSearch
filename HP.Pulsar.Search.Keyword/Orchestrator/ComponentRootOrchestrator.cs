using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ComponentRootOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }

    public ComponentRootOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
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
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(roots.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(KeywordSearchInfo.SearchEngineUrl, IndexTypeValue.ComponentRoot);
        await writer.SendIndexDeletionAsync(); //for test
        await writer.SendIndexCreationAsync();
        await writer.SendUpdateSettingAsync();
        await writer.SendUpdatePaginationAsync();
        await writer.SendElementsCreationAsync(roots);
        await writer.UpdateSearchableAttributesAsync(elementKeyContainer.Get());
    }
}
