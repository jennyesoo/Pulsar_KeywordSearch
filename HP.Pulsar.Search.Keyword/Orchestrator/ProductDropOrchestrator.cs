using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductDropOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo _keywordSearchInfo { get; }

    public ProductDropOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        _keywordSearchInfo = keywordSearchInfo;
    }

    public async Task InitializeAsync()
    {
        // read productDrops from database
        ProductDropReader reader = new(_keywordSearchInfo);
        IEnumerable<CommonDataModel> productDrops = await reader.GetDataAsync();

        // data processing
        ProductDropDataTransformer tranformer = new();
        productDrops = tranformer.Transform(productDrops);

        // summary property
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(productDrops.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.ProductDrop);
        await writer.InitializeIndexCreationStepsAsync(productDrops, elementKeyContainer.Get());
    }
}
