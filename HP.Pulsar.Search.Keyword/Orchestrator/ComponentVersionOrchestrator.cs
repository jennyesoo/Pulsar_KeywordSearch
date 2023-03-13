using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            IEnumerable<CommonDataModel> ComponentVersion = await reader.GetDataAsync();

            // data processing
            ComponentVersionDataTranformer tranformer = new();
            ComponentVersion = tranformer.Transform(ComponentVersion);

            // data to meiliesearch format
            List<Dictionary<string, string>> allComponentVersions = new();
            foreach (CommonDataModel rootversion in ComponentVersion)
            {
                allComponentVersions.Add(rootversion.GetAllData());
            }

            // write to meiliesearch
            MeiliSearchWriter _meilisearch = new(KeywordSearchInfo.SearchEngineUrl, "Pulsar2");
            await _meilisearch.CreateIndexAsync();
            DateTime start = DateTime.Now;
            await _meilisearch.UpsertAsync(allComponentVersions);
            DateTime end = DateTime.Now;
            Console.Write((end - start).TotalSeconds);

        }
    }
}
