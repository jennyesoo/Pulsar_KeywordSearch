using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class FeatureOrchestrator : IInitializationOrchestrator
{
    public FeatureOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
    }

    public KeywordSearchInfo KeywordSearchInfo { get; }

    public async Task InitializeAsync()
    {
        // read products from database
        FeatureReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> features = await reader.GetDataAsync();

        // data processing
        FeatureDataTransformer tranformer = new();
        features = tranformer.Transform(features);

        // summary property
        ElementKeyContainer.Add(features.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

        await writer.SendIndexCreationAsync();
        await writer.SendUpdateSettingAsync();
        await writer.SendUpdatePaginationAsync();

        await writer.SendElementsCreationAsync(features);
    }
}
