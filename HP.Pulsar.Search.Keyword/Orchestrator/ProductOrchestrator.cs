using System.Text.Json;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
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

    public async Task InitializeAsync()
    {
        // read products from database
        ProductReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> products = await reader.GetDataAsync();

        // data processing
        ProductDataTranformer tranformer = new();
        products = tranformer.Transform(products);

        // data to json
        List<Dictionary<string, string>> allProducts = new();
        foreach (CommonDataModel product in products)
        {
            allProducts.Add(product.GetAllData());
        }
        //string productJson = JsonSerializer.Serialize(allProducts);

        //// write to meiliesearch
        MeilisearchClient client = new(KeywordSearchInfo.SearchEngineUrl, "masterKey");
        _ = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        //IEnumerable<Dictionary<string, string>>? pulsar = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, string>>>(productJson);
        await client.DeleteIndexAsync("Pulsar2");
        Meilisearch.Index index = client.Index("Pulsar2");
        await client.CreateIndexAsync("Pulsar2", "Id");
        
        DateTime start = DateTime.Now;
        await index.AddDocumentsAsync(allProducts);
        DateTime end = DateTime.Now;

        Console.Write((end - start).TotalSeconds);

        //throw new NotImplementedException();
    }
}
