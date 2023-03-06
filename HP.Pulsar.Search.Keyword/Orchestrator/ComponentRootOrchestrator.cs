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

        public async Task InitializeAsync()
        {
            // read products from database
            ComponentRootReader reader = new(KeywordSearchInfo);

            IEnumerable<CommonDataModel> roots = await reader.GetDataAsync();

            // data processing
            ComponentRootTranformer tranformer = new();
            roots = tranformer.Transform(roots);

            // data to meiliesearch format
            List<Dictionary<string, string>> allComponentRoots = new();
            foreach (CommonDataModel ComponentRoot in roots)
            {
                allComponentRoots.Add(ComponentRoot.GetAllData());
            }

            //// write to meiliesearch
            MeiliSearchWriter _meilisearch = new(KeywordSearchInfo.SearchEngineUrl, "Pulsar2");
            DateTime start = DateTime.Now;
            await _meilisearch.UpsertAsync(allComponentRoots);
            DateTime end = DateTime.Now;
            Console.Write((end - start).TotalSeconds);

            //MeilisearchClient client = new(KeywordSearchInfo.SearchEngineUrl, "masterKey");
            //Meilisearch.Index index = client.Index("Pulsar2");
            //await index.AddDocumentsAsync(allComponentRoots);

            //throw new NotImplementedException();
        }
    }
}
