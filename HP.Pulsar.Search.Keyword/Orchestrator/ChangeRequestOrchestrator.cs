using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ChangeRequestOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }
    private MeiliSearchClient MeiliSearchClient { get; }

    public ChangeRequestOrchestrator(KeywordSearchInfo keywordSearchInfo, MeiliSearchClient meiliSearchClient)
    {
        KeywordSearchInfo = keywordSearchInfo;
        MeiliSearchClient = meiliSearchClient;
    }

    public async Task InitializeAsync()
    {
        // read changeRequests from database
        ChangeRequestReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> changeRequests = await reader.GetDataAsync();

        // data processing
        ChangeRequestDataTransformer tranformer = new();
        changeRequests = tranformer.Transform(changeRequests);

        // summary property
        ElementKeyContainer.Add(changeRequests.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        await MeiliSearchClient.SendElementsCreationAsync(changeRequests);
    }
}
