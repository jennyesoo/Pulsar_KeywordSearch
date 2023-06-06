using System.Text.RegularExpressions;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

public class SearchClient
{
    private readonly MeiliSearchClient _client;

    private static readonly List<Regex> _pattern = new()
        {
            //This pattern accepts special character in Avdetial
            new Regex(@"[A-Za-z0-9]{4}[0-9]{1}[A-Za-z]{2}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Avdetial
            new Regex(@"[A-Za-z0-9]{5}[A-Za-z]{1}[A-Za-z0-9]{1}#[A-Za-z0-9]{1}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Avdetial
            new Regex(@"[A-Za-z0-9]{3}[0-9]{2}AV#[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in PartNumber
            new Regex(@"[A-Za-z0-9]{1}[0-9]{2}[A-Za-z0-9]{3}\-[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in PartNumber
            new Regex(@"[A-Za-z]{3}[0-9]{2}[A-Za-z]{2}[A-Za-z0-9\#]{6}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in PartNumber
            new Regex(@"[A-Za-z]{3}[0-9]{4}[A-Za-z]{5}[A-Za-z0-9]{1}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in PartNumber
            new Regex(@"[A-Za-z]{3}[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{7}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in PartNumber
            new Regex(@"[A-Za-z]{3}[0-9]{4}[A-Za-z]{4}[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in PartNumber
            new Regex(@"IRS[A-Za-z]{6}[0-9]{6}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in PartNumber
            new Regex(@"[A-Za-z]{3}[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{8}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in MLName
            new Regex(@"[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{3}[A-Za-z]{1}[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in MLName
            new Regex(@"[0-9]{2}[A-Za-z0-9]{2}[A-Za-z]{3}[A-Za-z0-9\#]{3}", RegexOptions.IgnoreCase)
        };

    public SearchClient(KeywordSearchInfo info)
    {
        _client = new MeiliSearchClient(info.SearchEngineUrl, info.SearchEngineIndexName);
    }

    public async Task<IReadOnlyDictionary<SearchType, List<SingleOutputModel>>> SearchAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Dictionary<SearchType, List<SingleOutputModel>>();
        }

        //pre-process
        List<string> handledInput = PreProcess(input.Trim());

        SearchQuery searchQuery = new SearchQuery
        {
            MatchingStrategy = "all",
            Limit = 700
        };

        IReadOnlyDictionary<SearchType, List<SingleOutputModel>> models = await _client.SearchAsync(string.Join(" ", handledInput), searchQuery);

        // TODO - post-process 

        return models;
    }

    private static List<string> PreProcess(string input)
    {
        string[] inputs = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        List<string> handledInput = new();
        foreach (string temp in inputs)
        {
            //This pattern accepts special character such as "2-2-0" to avoid parsing to date
            if (Regex.IsMatch(temp, @"[0-9]+\-[0-9]+"))
            {
                handledInput.Add($"{temp}");
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
                handledInput.Add(temp);
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
