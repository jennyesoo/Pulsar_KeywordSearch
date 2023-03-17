using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.DataWriter;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ComponentRootOrchestrator : IInitializationOrchestrator
{
    public ComponentRootOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
    }

    public KeywordSearchInfo KeywordSearchInfo { get; }

    public async Task<int> InitializeAsync(int startId)
    {
        // read products from database
        ComponentRootReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> roots = await reader.GetDataAsync();

        // data processing
        ComponentRootTranformer tranformer = new();
        roots = tranformer.Transform(roots);

        // add meilisearch id
        foreach (CommonDataModel product in roots)
        {
            product.Add("Id", startId.ToString());
            startId++;
        }

        // write to meiliesearch
        MeiliSearchWriter writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

        if (!await writer.UidExistsAsync(KeywordSearchInfo.SearchEngineIndexName))
        {
            await writer.CreateIndexAsync();
            await writer.UpdateSettingAsync();
        }

        await writer.AddElementsAsync(roots);

        return startId;
    }
}
