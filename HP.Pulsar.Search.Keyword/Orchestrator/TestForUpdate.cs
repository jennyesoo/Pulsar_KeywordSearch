using System;
using System.Collections.Generic;
using System.Text;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using HP.Pulsar.Search.Keyword.SearchEngine;

namespace HP.Pulsar.Search.Keyword.Orchestrator
{
    public class TestForUpdate
    {
        private KeywordSearchInfo _keywordSearchInfo { get; }

        public TestForUpdate(KeywordSearchInfo keywordSearchInfo)
        {
            _keywordSearchInfo = keywordSearchInfo;
        }

        public async Task UpdateAsync()
        {
            //await UpdateProductAsync(3124);
            //await UpdateProductDropAsync(1234);
            //await UpdateHpAMOPartNumberAsync(1234);
            //await UpdateFeatureAsync(1234);
            await UpdateVersionAsync(113087);
            //await UpdateRootAsync(12342);
            //await UpdateChangeRequestAsync(1234);
        }

        private async Task UpdateChangeRequestAsync(int num)
        {
            ChangeRequestReader reader = new(_keywordSearchInfo);
            CommonDataModel changeRequest = await reader.GetDataAsync(num);

            if (changeRequest is null)
            {
                return;
            }

            // data processing
            ChangeRequestDataTransformer tranformer = new();
            changeRequest = tranformer.Transform(changeRequest);

            // write to meiliesearch
            MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.Dcr);
            await writer.SendElementUpdationAsync(changeRequest);
        }

        private async Task UpdateRootAsync(int num)
        {
            ComponentRootReader reader = new(_keywordSearchInfo);
            CommonDataModel root = await reader.GetDataAsync(num);

            if (root is null)
            {
                return;
            }

            // data processing
            ComponentRootTransformer tranformer = new();
            root = tranformer.Transform(root);

            // write to meiliesearch
            MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.ComponentRoot);
            await writer.SendElementUpdationAsync(root);
        }

        private async Task UpdateVersionAsync(int num)
        {
            ComponentVersionReader reader = new(_keywordSearchInfo);
            CommonDataModel version = await reader.GetDataAsync(num);

            if (version is null)
            {
                return;
            }

            // data processing
            ComponentVersionDataTransformer tranformer = new();
            version = tranformer.Transform(version);

            // write to meiliesearch
            MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.ComponentVersion);
            await writer.SendElementUpdationAsync(version);
        }

        private async Task UpdateFeatureAsync(int num)
        {
            FeatureReader reader = new(_keywordSearchInfo);
            CommonDataModel feature = await reader.GetDataAsync(num);

            if (feature is null)
            {
                return;
            }

            // data processing
            FeatureDataTransformer tranformer = new();
            feature = tranformer.Transform(feature);

            // write to meiliesearch
            MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.Feature);
            await writer.SendElementUpdationAsync(feature);
        }

        private async Task UpdateHpAMOPartNumberAsync(int num)
        {
            HpAMOPartNumberReader reader = new(_keywordSearchInfo);
            CommonDataModel hpAMOPartNumber = await reader.GetDataAsync(num);

            if (hpAMOPartNumber is null)
            {
                return;
            }

            // data processing
            HpAMOPartNumberDataTransformer tranformer = new();
            hpAMOPartNumber = tranformer.Transform(hpAMOPartNumber);

            // write to meiliesearch
            MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.AmoPartNumber);
            await writer.SendElementUpdationAsync(hpAMOPartNumber);
        }

        private async Task UpdateProductAsync(int num)
        {
            ProductReader reader = new(_keywordSearchInfo);
            CommonDataModel product = await reader.GetDataAsync(num);

            if (product is null)
            {
                return;
            }

            // data processing
            ProductDataTransformer tranformer = new();
            product = tranformer.Transform(product);

            // write to meiliesearch
            MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.Product);
            await writer.SendElementUpdationAsync(product);
        }

        private async Task UpdateProductDropAsync(int num)
        {
            ProductDropReader reader = new(_keywordSearchInfo);
            CommonDataModel productDrop = await reader.GetDataAsync(num);

            if (productDrop is null)
            {
                return;
            }

            // data processing
            ProductDropDataTransformer tranformer = new();
            productDrop = tranformer.Transform(productDrop);

            // write to meiliesearch
            MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.ProductDrop);
            await writer.SendElementUpdationAsync(productDrop);
        }
    }
}
