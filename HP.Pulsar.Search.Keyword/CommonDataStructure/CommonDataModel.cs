using System.Collections.Concurrent;

namespace HP.Pulsar.Search.Keyword.CommonDataStructure;

public class CommonDataModel
{
    private readonly ConcurrentDictionary<string, string> _pairs;

    public CommonDataModel()
    {
        _pairs = new(StringComparer.OrdinalIgnoreCase);
    }

    public void Add(string key, string value)
    {
        _pairs[key] = value;
    }

    public void Delete(string key)
    {
        _pairs.Remove(key, out _);
    }

    public IEnumerable<(string, string)> Get()
    {
        return _pairs.Select(x => (x.Key, x.Value)).ToList();
    }

    public string GetValue(string key)
    {
        if (_pairs.ContainsKey(key))
        {
            return _pairs[key];
        }

        return string.Empty;
    }

    public ICollection<string> GetAllKeys()
    {
        return _pairs.Keys;
    }

    public IReadOnlyDictionary<string, string> GetAllData()
    {
        return _pairs;
    }
}
