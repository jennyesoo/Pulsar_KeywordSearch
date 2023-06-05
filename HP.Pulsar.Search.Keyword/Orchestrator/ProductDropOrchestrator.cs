using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductDropOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }
    private MeiliSearchClient MeiliSearchClient { get; }

    public ProductDropOrchestrator(KeywordSearchInfo keywordSearchInfo, MeiliSearchClient meiliSearchClient)
    {
        KeywordSearchInfo = keywordSearchInfo;
        MeiliSearchClient = meiliSearchClient;
    }

    public async Task InitializeAsync()
    {
        // read productDrops from database
        ProductDropReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> productDrops = await reader.GetDataAsync();

        // data processing
        ProductDropDataTransformer tranformer = new();
        productDrops = tranformer.Transform(productDrops);

        // summary property
        ElementKeyContainer.Add(productDrops.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        await MeiliSearchClient.SendElementsCreationAsync(productDrops);
    }
}
