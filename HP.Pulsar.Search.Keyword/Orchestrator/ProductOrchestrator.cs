using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using System.Text.Json;
using System.IO;
using Meilisearch;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

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
        CommonDataTranformer tranformer = new();
        products = tranformer.Transform(products);

        // data to json
        List<Dictionary<string, string>> AllProduct = new();
        foreach (CommonDataModel product in products)
        {
            AllProduct.Add(product.GetAllData());
        }
        string DatajsonString = JsonSerializer.Serialize(AllProduct);

        //// write to meiliesearch
        MeilisearchClient client = new MeilisearchClient(KeywordSearchInfo.SearchEngineUrl, "masterKey");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var pulsar = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, string>>>(DatajsonString);
        await client.DeleteIndexAsync("Pulsar2");
        var index = client.Index("Pulsar2");
        await client.CreateIndexAsync("Pulsar2", "Id");
        await index.AddDocumentsAsync<Dictionary<string, string>>(pulsar);

        Console.ReadLine();

        //throw new NotImplementedException();
    }
}
