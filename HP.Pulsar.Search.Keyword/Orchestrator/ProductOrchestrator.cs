using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using System.Collections.Generic;
using System.Text.Json;
using static HP.Pulsar.Search.Keyword.Orchestrator.ProductOrchestrator;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;
using Meilisearch;
using System.Threading.Tasks;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductOrchestrator : IInitializationOrchestrator
{
    public ProductOrchestrator()
    {

    }

    public Task InitializeAsync()
    {
        ProductReader Product = new ProductReader(PulsarEnvironment.Test);
        DataProcessing dataProcessing = new DataProcessing();
        // read products from database
        IEnumerable<ProductDataModel>  Data = (IEnumerable<ProductDataModel>)Product.GetProductsAsync();

        // data processing
        Data = dataProcessing.GetDataProcessingData(Data);

        // data to json
        string DatajsonString = JsonSerializer.Serialize(Data);

        // write to meiliesearch
        MeilisearchClient client = new MeilisearchClient("http://localhost:7700", "masterKey");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var pulsar = JsonSerializer.Deserialize<IEnumerable<ProductDataModel>>(DatajsonString, options);
        var index = client.Index("Pulsar");
        index.AddDocumentsAsync<ProductDataModel>(pulsar);

        //throw new NotImplementedException();

        /*
        foreach (ProductDataModel item in Data)
        {
            Console.WriteLine(item.EndOfProduction);
        }
        */
    }
}
