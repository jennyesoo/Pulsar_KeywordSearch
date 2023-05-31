using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Humanizer;
using Meilisearch;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

public class SearchClient
{
    private readonly MeiliSearchClient _client;

    private static readonly List<Regex> _pattern = new()
        {
            //This pattern accepts special character such as "17WWQ1AL6AC", "17wwyzw6#d", "17WWQ1AD3##", "3C17"
            new Regex(@".*(([A-Za-z]+[0-9]+)+|([0-9]+[A-Za-z]+)+).*", RegexOptions.IgnoreCase),
            //This pattern accepts special character such as "ext.15973", "1.0", "1.6.0.17", "1.2.281.8344"
            new Regex(@"[A-Za-z0-9]+[\.]+[0-9]+", RegexOptions.IgnoreCase),
            //This pattern accepts special character such as "+886-3-327-2345"
            new Regex(@"[\+]*[0-9]+[\-]+[0-9]+[\-]+[0-9]+[\-]+[0-9]+", RegexOptions.IgnoreCase)
        };

    public SearchClient(KeywordSearchInfo info)
    {
        _client = new MeiliSearchClient(info.SearchEngineUrl, info.SearchEngineIndexName);
    }

    public async Task<IReadOnlyDictionary<SearchType, List<SingleOutputModel>>> SearchAsync(string input)
    {
        //pre-process
        List<string> handledInput = PreProcess(input);

        SearchQuery searchQuery = new SearchQuery
        {
            MatchingStrategy = "all",
            Limit = 700
        };

        IReadOnlyDictionary<SearchType, List<SingleOutputModel>> models = await _client.SearchAsync(string.Join(' ', handledInput), searchQuery);

        // TODO - post-process 

        return models;
    }

    private static List<string> PreProcess(string input)
    {
        string[] inputs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        List<string> handledInput = new();
        foreach (string temp in inputs)
        {
            //This pattern accepts special character such as "2-2-0"
            if (Regex.IsMatch(temp, @"[0-9]+\-[0-9]+"))
            {
                handledInput.Add($"\"{temp}\"");
            }
            else if (CommonDataTransformer.TryParseDate(temp, out DateTime date))
            {
                handledInput.Add($"\"{date:yyyy/MM/dd}\"");
            }
            else if (int.TryParse(temp, out _))
            {
                handledInput.Add($"\"{temp}\"");
            }
            else if (GetRexAnswer(temp, out string result))
            {
                handledInput.Add(result);
            }
            else
            {
                handledInput.Add(temp.Singularize());
            }
        }

        return handledInput;
    }

    public static bool GetRexAnswer(string userQuestionTemp, out string result)
    {
        result = userQuestionTemp;

        for (int i = 0; i < _pattern.Count; i++)
        {
            if (_pattern[i].IsMatch(userQuestionTemp.Trim()))
            {
                result = $"\"{userQuestionTemp}\"";
                return true;
            }
        }
        return false;
    }
}
