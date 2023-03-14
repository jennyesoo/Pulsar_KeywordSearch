using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.DataWriter;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.Orchestrator
{
    internal class ComponentRootOrchestrator : IInitializationOrchestrator
    {
        public ComponentRootOrchestrator(KeywordSearchInfo keywordSearchInfo)
        {
            KeywordSearchInfo = keywordSearchInfo;
        }

        public KeywordSearchInfo KeywordSearchInfo { get; }

        public async Task<int> InitializeAsync(int _meilisearchcount)
        {
            // read products from database
            ComponentRootReader reader = new(KeywordSearchInfo);
            IEnumerable<CommonDataModel> roots = await reader.GetDataAsync();

            // data processing
            ComponentRootTranformer tranformer = new();
            roots = tranformer.Transform(roots);

            // data to meiliesearch format
            List<Dictionary<string, string>> allComponentRoots = new();
            foreach (CommonDataModel ComponentRoot in roots) //74282 items
            {
                _meilisearchcount++;
                ComponentRoot.Add("Id", _meilisearchcount.ToString());
                allComponentRoots.Add(ComponentRoot.GetAllData());
            }

            //// write to meiliesearch
            MeiliSearchWriter _meilisearch = new(KeywordSearchInfo.SearchEngineUrl, "Pulsar2");
            await _meilisearch.UpsertAsync(allComponentRoots); //7.59s
            return _meilisearchcount;
        }
    }
}
