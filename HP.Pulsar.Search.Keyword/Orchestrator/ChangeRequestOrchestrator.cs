using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ChangeRequestOrchestrator : IInitializationOrchestrator
{
    public ChangeRequestOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
    }

    public KeywordSearchInfo KeywordSearchInfo { get; }

    public async Task InitializeAsync()
    {
        // read products from database
        ChangeRequestReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> changeRequests = await reader.GetDataAsync();

        // data processing
        ChangeRequestDataTransformer tranformer = new();
        changeRequests = tranformer.Transform(changeRequests);

        // summary property
        ElementKeyContainer.Add(changeRequests.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient mClient = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

        await mClient.SendIndexCreationAsync();
        await mClient.SendUpdateSettingAsync();
        await mClient.SendUpdatePaginationAsync();

        await mClient.SendElementsCreationAsync(changeRequests);
    }
}
