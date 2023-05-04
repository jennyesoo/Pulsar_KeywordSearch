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
    internal class HpAMOPartNumberOrchestrator : IInitializationOrchestrator
    {
        public HpAMOPartNumberOrchestrator(KeywordSearchInfo keywordSearchInfo)
        {
            KeywordSearchInfo = keywordSearchInfo;
        }

        public KeywordSearchInfo KeywordSearchInfo { get; }

        public async Task InitializeAsync()
        {
            // read products from database
            HpAMOPartNumberReader reader = new(KeywordSearchInfo);
            IEnumerable<CommonDataModel> hpAMOPartNumber = await reader.GetDataAsync();

            // data processing
            HpAMOPartNumberDataTransformer tranformer = new();
            hpAMOPartNumber = tranformer.Transform(hpAMOPartNumber);

            // write to meiliesearch
            MeiliSearchWriter writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

            if (!await writer.UidExistsAsync(KeywordSearchInfo.SearchEngineIndexName))
            {
                await writer.CreateIndexAsync();
                await writer.UpdateSettingAsync();
            }

            await writer.AddElementsAsync(hpAMOPartNumber);
        }
    }
}

