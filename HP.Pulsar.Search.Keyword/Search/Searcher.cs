using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.Search;

public class Searcher
{
    private readonly KeywordSearchInfo _info;
    private readonly MeilisearchClient _client;

    public Searcher(KeywordSearchInfo info)
    {
        _info = info;
        _client = new(info.SearchEngineUrl, "masterKey");
    }

    public string Search(string input)
    {
        IEnumerable<string> temp = SearchInputFilter.Filter(input.Split(' '));


        return "";
    }

}
