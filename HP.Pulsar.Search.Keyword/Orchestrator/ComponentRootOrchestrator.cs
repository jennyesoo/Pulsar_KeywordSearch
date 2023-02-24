using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
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

            //set meilisearch count 
            //KeywordSearchInfo.MeilisearchCount = allComponentRoots.Count;

            //// write to meiliesearch
            MeilisearchClient client = new(KeywordSearchInfo.SearchEngineUrl, "masterKey");
            //await client.DeleteIndexAsync("Pulsar2");
            Meilisearch.Index index = client.Index("Pulsar2");
            //await client.CreateIndexAsync("Pulsar2", "Id");

            DateTime start = DateTime.Now;
            await index.AddDocumentsAsync(allComponentRoots);
            DateTime end = DateTime.Now;

            Console.Write((end - start).TotalSeconds);

            //throw new NotImplementedException();
        }
    }
}
