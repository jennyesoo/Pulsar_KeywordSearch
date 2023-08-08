using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ComponentVersionOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo _keywordSearchInfo { get; }

    public ComponentVersionOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        _keywordSearchInfo = keywordSearchInfo;
    }

    public async Task InitializeAsync()
    {
        // read componentversion from database
        ComponentVersionReader reader = new(_keywordSearchInfo);
        IEnumerable<CommonDataModel> versions = await reader.GetDataAsync();

        // data processing
        ComponentVersionDataTransformer tranformer = new();
        versions = tranformer.Transform(versions);

        // summary property
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(versions.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.ComponentVersion);
        await writer.InitializeIndexCreationStepsAsync(versions, elementKeyContainer.Get(), 8000);
    }
}
