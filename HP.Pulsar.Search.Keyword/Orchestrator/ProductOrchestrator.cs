using System.Text.Json;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.DataWriter;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductOrchestrator : IInitializationOrchestrator
{
    public ProductOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
    }

    public KeywordSearchInfo KeywordSearchInfo { get; }

    public async Task<int> InitializeAsync(int _meilisearchcount)
    {
        // read products from database
        ProductReader reader = new(KeywordSearchInfo);

        IEnumerable<CommonDataModel> products = await reader.GetDataAsync();

        // data processing
        ProductDataTranformer tranformer = new();
        products = tranformer.Transform(products);

        // data to meiliesearch format and add meilisearch id 
        List<Dictionary<string, string>> allProducts = new();
        foreach (CommonDataModel product in products)
        {
            _meilisearchcount ++;
            product.Add("Id", _meilisearchcount.ToString());
            allProducts.Add(product.GetAllData());
        }

        // write to meiliesearch
        MeiliSearchWriter _meilisearch = new(KeywordSearchInfo.SearchEngineUrl, "Pulsar2");
        await _meilisearch.CreateIndexAsync();
        DateTime start = DateTime.Now;
        await _meilisearch.UpsertAsync(allProducts);
        DateTime end = DateTime.Now;
        Console.Write((end - start).TotalSeconds);

        return _meilisearchcount;
        //MeilisearchClient client = new(KeywordSearchInfo.SearchEngineUrl, "masterKey");
        //await client.DeleteIndexAsync("Pulsar2");
        //await client.CreateIndexAsync("Pulsar2", "Id");
        //Meilisearch.Index index = client.Index("Pulsar2");
        //await index.AddDocumentsAsync(allProducts);

        //throw new NotImplementedException();
    }
}
