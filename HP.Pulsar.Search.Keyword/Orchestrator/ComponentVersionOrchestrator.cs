﻿using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataReader;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

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

        // summary property
        ElementKeyContainer.Add(versions.SelectMany(p => p.GetKeys()).Distinct<string>());

        // write to meiliesearch
        MeiliSearchClient writer = new(KeywordSearchInfo.SearchEngineUrl, KeywordSearchInfo.SearchEngineIndexName);

        await writer.SendElementsCreationAsync(versions);
    }
}
