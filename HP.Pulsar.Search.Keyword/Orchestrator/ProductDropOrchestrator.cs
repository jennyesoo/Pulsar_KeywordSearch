using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductDropOrchestrator : IInitializationOrchestrator
{
    public ProductDropOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
    }

    public KeywordSearchInfo KeywordSearchInfo { get; }

    public async Task InitializeAsync()
    {
        // read products from database
        ProductDropReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> productDrops = await reader.GetDataAsync();

        // data processing
        ProductDropDataTransformer tranformer = new();
        productDrops = tranformer.Transform(productDrops);

        // summary property
        ElementKeyContainer.Add(productDrops.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

        await writer.SendIndexCreationAsync();
        await writer.SendUpdateSettingAsync();
        await writer.SendUpdatePaginationAsync();

        await writer.SendElementsCreationAsync(productDrops);
    }
}
