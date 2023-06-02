using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class HpAMOPartNumberOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo KeywordSearchInfo { get; }
    private MeiliSearchClient MeiliSearchClient { get; }

    public HpAMOPartNumberOrchestrator(KeywordSearchInfo keywordSearchInfo, MeiliSearchClient meiliSearchClient)
    {
        KeywordSearchInfo = keywordSearchInfo;
        MeiliSearchClient = meiliSearchClient;
    }

    public async Task InitializeAsync()
    {
        // read products from database
        HpAMOPartNumberReader reader = new(KeywordSearchInfo);
        IEnumerable<CommonDataModel> hpAMOPartNumber = await reader.GetDataAsync();

        // data processing
        HpAMOPartNumberDataTransformer transformer = new();
        hpAMOPartNumber = transformer.Transform(hpAMOPartNumber);

        // summary property
        ElementKeyContainer.Add(hpAMOPartNumber.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        await MeiliSearchClient.SendElementsCreationAsync(hpAMOPartNumber);
    }
}

