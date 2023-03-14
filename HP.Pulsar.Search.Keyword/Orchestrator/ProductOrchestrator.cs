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
        foreach (CommonDataModel product in products) //3241 items
        {
            _meilisearchcount ++;
            product.Add("Id", _meilisearchcount.ToString());
            allProducts.Add(product.GetAllData());
        }

        // write to meiliesearch
        MeiliSearchWriter _meilisearch = new(KeywordSearchInfo.SearchEngineUrl, "Pulsar2");
        await _meilisearch.DeleteIndexAsync();
        await _meilisearch.CreateIndexAsync();
        await _meilisearch.UpdateSetting();
        await _meilisearch.UpsertAsync(allProducts); //0.75s

        return _meilisearchcount;
    }
}
