using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.DataWriter;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator
{
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
            MeiliSearchWriter writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

            if (!await writer.UidExistsAsync(KeywordSearchInfo.SearchEngineIndexName))
            {
                await writer.CreateIndexAsync();
                await writer.UpdateSettingAsync();
                await writer.UpdatePaginationAsync();
            }

            await writer.AddElementsAsync(changeRequests);
        }
    }
}
