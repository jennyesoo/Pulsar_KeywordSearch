using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.DataWriter;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductOrchestrator : IInitializationOrchestrator
{

    public ProductOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
    }

    public KeywordSearchInfo KeywordSearchInfo { get; }

    public async Task<int> InitializeAsync(int startId)
    {
        // read products from database
        ProductReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> products = await reader.GetDataAsync();

        // data processing
        ProductDataTranformer tranformer = new();
        products = tranformer.Transform(products);

        // add meilisearch id
        foreach(CommonDataModel product in products)
        {
            product.Add("Id", startId.ToString());
            startId++;
        }

        // write to meiliesearch
        MeiliSearchWriter writer3 = new(KeywordSearchInfo.SearchEngineUrl, "Pulsar3");
        await writer3.DeleteIndexAsync();

        MeiliSearchWriter writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

        if (!await writer.UidExistsAsync(KeywordSearchInfo.SearchEngineIndexName))
        {
            await writer.CreateIndexAsync();
            await writer.UpdateSettingAsync();
        }

        await writer.AddElementsAsync(products);

        return startId;
    }
}
