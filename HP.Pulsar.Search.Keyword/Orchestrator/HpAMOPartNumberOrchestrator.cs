using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class HpAMOPartNumberOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo _keywordSearchInfo { get; }

    public HpAMOPartNumberOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        _keywordSearchInfo = keywordSearchInfo;
    }

    public async Task InitializeAsync()
    {
        // read hpAMOPartNumber from database
        HpAMOPartNumberReader reader = new(_keywordSearchInfo);
        IEnumerable<CommonDataModel> hpAMOPartNumber = await reader.GetDataAsync();

        // data processing
        HpAMOPartNumberDataTransformer transformer = new();
        hpAMOPartNumber = transformer.Transform(hpAMOPartNumber);

        // summary property
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(hpAMOPartNumber.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.AmoPartNumber);
        await writer.InitializeIndexCreationStepsAsync(hpAMOPartNumber, elementKeyContainer.Get(), 10000);
    }
}