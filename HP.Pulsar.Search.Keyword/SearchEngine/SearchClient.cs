using System.Text.RegularExpressions;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

public class SearchClient
{
    private readonly MeiliSearchClient _productClient;
    private readonly MeiliSearchClient _rootClient;
    private readonly MeiliSearchClient _versionClient;
    private readonly MeiliSearchClient _dcrClient;
    private readonly MeiliSearchClient _productDropClient;
    private readonly MeiliSearchClient _featureClient;
    private readonly MeiliSearchClient _amoPartNumberClient;

    private static readonly List<Regex> _pattern = new()
        {
            //This pattern accepts special character in Av detial
            new Regex(@"[A-Za-z0-9]{4}[0-9]{1}[A-Za-z]{2}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Av detial
            new Regex(@"[A-Za-z0-9]{5}[A-Za-z]{1}[A-Za-z0-9]{1}#[A-Za-z0-9]{1}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Av detial
            new Regex(@"[A-Za-z0-9]{3}[0-9]{2}AV#[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Part Number
            new Regex(@"[A-Za-z0-9]{1}[0-9]{2}[A-Za-z0-9]{3}\-[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Part Number
            new Regex(@"[A-Za-z]{3}[0-9]{2}[A-Za-z]{2}[A-Za-z0-9\#]{6}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Part Number
            new Regex(@"[A-Za-z]{3}[0-9]{4}[A-Za-z]{5}[A-Za-z0-9]{1}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Part Number
            new Regex(@"[A-Za-z]{3}[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{7}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Part Number
            new Regex(@"[A-Za-z]{3}[0-9]{4}[A-Za-z]{4}[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Part Number
            new Regex(@"IRS[A-Za-z]{6}[0-9]{6}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in Part Number
            new Regex(@"[A-Za-z]{3}[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{8}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in ML Name
            new Regex(@"[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{3}[A-Za-z]{1}[A-Za-z0-9]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character in ML Name
            new Regex(@"[0-9]{2}[A-Za-z0-9]{2}[A-Za-z]{3}[A-Za-z0-9\#]{3}", RegexOptions.IgnoreCase),
            //This pattern accepts special character with English word and number
            new Regex(@".*(([A-Za-z]+[0-9]+)+|([0-9]+[A-Za-z]+)+).*", RegexOptions.IgnoreCase)
        };

    public SearchClient(KeywordSearchInfo info)
    {
        _productClient = new MeiliSearchClient(info.SearchEngineUrl, IndexName.Product);
        _rootClient = new MeiliSearchClient(info.SearchEngineUrl, IndexName.ComponentRoot);
        _versionClient = new MeiliSearchClient(info.SearchEngineUrl, IndexName.ComponentVersion);
        _dcrClient = new MeiliSearchClient(info.SearchEngineUrl, IndexName.Dcr);
        _productDropClient = new MeiliSearchClient(info.SearchEngineUrl, IndexName.ProductDrop);
        _featureClient = new MeiliSearchClient(info.SearchEngineUrl, IndexName.Feature);
        _amoPartNumberClient = new MeiliSearchClient(info.SearchEngineUrl, IndexName.AmoPartNumber);
    }

    public async Task<IReadOnlyDictionary<SearchType, IEnumerable<SingleOutputModel>>> SearchAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Dictionary<SearchType, IEnumerable<SingleOutputModel>>();
        }

        //pre-process
        List<string> handledInput = PreProcess(input.Trim());

        SearchParameters searchQuery = new()
        {
            Q = string.Join(" ", handledInput)
        };

        Task<IEnumerable<SingleOutputModel>>[] tasks = new Task<IEnumerable<SingleOutputModel>>[7];
        tasks[0] = _productClient.SearchAsync(searchQuery);
        tasks[1] = _rootClient.SearchAsync(searchQuery);
        tasks[2] = _versionClient.SearchAsync(searchQuery);
        tasks[3] = _dcrClient.SearchAsync(searchQuery);
        tasks[4] = _productDropClient.SearchAsync(searchQuery);
        tasks[5] = _featureClient.SearchAsync(searchQuery);
        tasks[6] = _amoPartNumberClient.SearchAsync(searchQuery);

        await Task.WhenAll(tasks);

        // TODO - post-process 

        Dictionary<SearchType, IEnumerable<SingleOutputModel>> models = new();
        models[SearchType.Product] = tasks[0].Result;
        models[SearchType.Root] = tasks[1].Result;
        models[SearchType.Version] = tasks[2].Result;
        models[SearchType.DCR] = tasks[3].Result;
        models[SearchType.ProductDrop] = tasks[4].Result;
        models[SearchType.Feature] = tasks[5].Result;
        models[SearchType.AmoPartNumber] = tasks[6].Result;

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
