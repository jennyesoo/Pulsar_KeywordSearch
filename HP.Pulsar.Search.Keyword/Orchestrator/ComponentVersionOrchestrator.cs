using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.DataWriter;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator
{
    internal class ComponentVersionOrchestrator : IInitializationOrchestrator
    {
        public ComponentVersionOrchestrator(KeywordSearchInfo keywordSearchInfo)
        {
            KeywordSearchInfo = keywordSearchInfo;
        }

        public KeywordSearchInfo KeywordSearchInfo { get; }

        public async Task InitializeAsync()
        {
            // read componentversion from database
            ComponentVersionReader reader = new(KeywordSearchInfo);
            IEnumerable<CommonDataModel> versions = await reader.GetDataAsync();

            // data processing
            ComponentVersionDataTransformer tranformer = new();
            versions = tranformer.Transform(versions);

            // write to meiliesearch
            MeiliSearchWriter writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

            if (!await writer.UidExistsAsync(KeywordSearchInfo.SearchEngineIndexName))
            {
                await writer.CreateIndexAsync();
                await writer.UpdateSettingAsync();
            }

            await writer.AddElementsAsync(versions);
        }
    }
}
