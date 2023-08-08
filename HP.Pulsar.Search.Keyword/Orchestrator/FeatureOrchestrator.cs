﻿using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

internal class FeatureOrchestrator : IInitializationOrchestrator
{
    private KeywordSearchInfo _keywordSearchInfo { get; }

    public FeatureOrchestrator(KeywordSearchInfo keywordSearchInfo)
    {
        _keywordSearchInfo = keywordSearchInfo;
    }

    public async Task InitializeAsync()
    {
        // read features from database
        FeatureReader reader = new(_keywordSearchInfo);
        IEnumerable<CommonDataModel> features = await reader.GetDataAsync();

        // data processing
        FeatureDataTransformer tranformer = new();
        features = tranformer.Transform(features);

        // summary property
        ElementKeyContainer elementKeyContainer = new();
        elementKeyContainer.Add(features.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, IndexName.Feature);
        await writer.InitializeIndexCreationStepsAsync(features, elementKeyContainer.Get(), 10000);
    }
}
