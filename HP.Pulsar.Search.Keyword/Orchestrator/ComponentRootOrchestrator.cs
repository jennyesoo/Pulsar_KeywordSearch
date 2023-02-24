using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            IEnumerable<CommonDataModel> ComponentRoots = await reader.GetDataAsync(KeywordSearchInfo.MeilisearchCount);

            // data processing
            ComponentRootTranformer tranformer = new();
            ComponentRoots = tranformer.Transform(ComponentRoots);

            // data to meiliesearch format
            List<Dictionary<string, string>> allComponentRoots = new();
            foreach (CommonDataModel ComponentRoot in ComponentRoots)
            {
                allComponentRoots.Add(ComponentRoot.GetAllData());
            }

            //set meilisearch count 
            KeywordSearchInfo.MeilisearchCount = allComponentRoots.Count;

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
