using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ProductDropOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }

    public ProductDropOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
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
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(productDrops.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(KeywordSearchInfo.SearchEngineUrl, IndexTypeValue.ProductDrop);
        //await writer.SendIndexDeletionAsync(); //for test
        await writer.SendIndexCreationAsync();
        await writer.SendUpdateSettingAsync();
        await writer.SendUpdatePaginationAsync();
        await writer.SendElementsCreationAsync(productDrops);
        await writer.UpdateSearchableAttributesAsync(elementKeyContainer.Get());
    }
}
