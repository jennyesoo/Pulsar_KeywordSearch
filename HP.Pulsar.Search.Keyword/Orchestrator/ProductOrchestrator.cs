using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }
    private MeiliSearchClient MeiliSearchClient { get; }

    public ProductOrchestrator(KeywordSearchInfo keywordSearchInfo, MeiliSearchClient meiliSearchClient)
    {
        KeywordSearchInfo = keywordSearchInfo;
        MeiliSearchClient = meiliSearchClient;
    }

    public async Task InitializeAsync()
    {
        // read products from database
        ProductReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> products = await reader.GetDataAsync();

        // data processing
        ProductDataTransformer tranformer = new();
        products = tranformer.Transform(products);

        // summary property
        ElementKeyContainer.Add(products.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        await MeiliSearchClient.SendElementsCreationAsync(products);
    }
}
