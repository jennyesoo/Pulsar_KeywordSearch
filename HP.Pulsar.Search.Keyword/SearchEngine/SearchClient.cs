using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

public class SearchClient
{
    private readonly MeiliSearchClient _client;

    public SearchClient(KeywordSearchInfo info)
    {
        _client = new MeiliSearchClient(info.SearchEngineUrl, info.SearchEngineIndexName);
    }

    public async Task<IReadOnlyDictionary<SearchType, List<SingleOutputModel>>> SearchAsync(string input)
    {
        // TODO - pre-process
        List<string> handledInput = PreProcess(input);

        // search 
        SearchQuery query = new();
        IReadOnlyDictionary<SearchType, List<SingleOutputModel>> models = await _client.SearchAsync(string.Join(' ', handledInput), query);

        // TODO - post-process 

        return models;
    }

    private static List<string> PreProcess(string input)
    {
        // TODO - remove double quotes in input
        string[] inputs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        List<string> handledInput = new();

        // datetime 
        foreach (string temp in inputs)
        {
            if (CommonDataTransformer.TryParseDate(temp, out DateTime date))
            {
                handledInput.Add($"\"{date:yyyy/MM/dd}\"");
            }
            else
            {
                handledInput.Add(temp);
            }
        }

        return handledInput;
    }
}
