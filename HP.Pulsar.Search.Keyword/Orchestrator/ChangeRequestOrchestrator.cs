using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class ChangeRequestOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }

    public ChangeRequestOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        KeywordSearchInfo = keywordSearchInfo;
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
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(changeRequests.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(KeywordSearchInfo.SearchEngineUrl, IndexTypeValue.Dcr);
        //await writer.SendIndexDeletionAsync(); //for test
        await writer.SendIndexCreationAsync();
        await writer.SendUpdateSettingAsync();
        await writer.SendUpdatePaginationAsync();
        await writer.SendElementsCreationAsync(changeRequests);
        await writer.UpdateSearchableAttributesAsync(elementKeyContainer.Get());
    }
}
