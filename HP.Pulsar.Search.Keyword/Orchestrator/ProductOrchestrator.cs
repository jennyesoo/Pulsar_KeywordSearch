using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;

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
        //string DatajsonString = JsonSerializer.Serialize(Data);

        //// write to meiliesearch
        //MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "masterKey");
        //var options = new JsonSerializerOptions
        //{
        //    PropertyNameCaseInsensitive = true
        //};

        //var pulsar = JsonSerializer.Deserialize<IEnumerable<ProductDataModel>>(DatajsonString, options);
        //var index = client.Index("Pulsar");
        //index.AddDocumentsAsync<ProductDataModel>(pulsar);

        //throw new NotImplementedException();

        /*
        foreach (ProductDataModel item in Data)
        {
            Console.WriteLine(item.EndOfProduction);
        }
        */
    }
}
