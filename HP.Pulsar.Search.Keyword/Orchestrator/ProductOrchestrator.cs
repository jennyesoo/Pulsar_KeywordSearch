using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo _keywordSearchInfo { get; }

    public ProductOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        _keywordSearchInfo = keywordSearchInfo;
    }

    public async Task InitializeAsync()
    {
        // read products from database
        ProductReader reader = new(_keywordSearchInfo);
        IEnumerable<CommonDataModel> products = await reader.GetDataAsync();

        // data processing
        ProductDataTransformer tranformer = new();
        products = tranformer.Transform(products);

        // summary property
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(products.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.Product);
        await writer.InitializeIndexCreationStepsAsync(products, elementKeyContainer.Get(), 500);
    }
}
