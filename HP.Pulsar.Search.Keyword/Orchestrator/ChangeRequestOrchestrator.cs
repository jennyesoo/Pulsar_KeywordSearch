using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ChangeRequestOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo _keywordSearchInfo { get; }

    public ChangeRequestOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        _keywordSearchInfo = keywordSearchInfo;
    }

    public async Task InitializeAsync()
    {
        // read changeRequests from database
        ChangeRequestReader reader = new(_keywordSearchInfo);
        IEnumerable<CommonDataModel> changeRequests = await reader.GetDataAsync();

        // data processing
        ChangeRequestDataTransformer tranformer = new();
        changeRequests = tranformer.Transform(changeRequests);

        // summary property
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(changeRequests.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.Dcr);
        await writer.InitializeIndexCreationStepsAsync(changeRequests, elementKeyContainer.Get());
    }
}
