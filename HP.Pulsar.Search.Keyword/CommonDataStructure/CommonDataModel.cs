namespace HP.Pulsar.Search.Keyword.CommonDataStructure;

public class CommonDataModel
{
    private readonly Dictionary<string, string> _pairs;

    public CommonDataModel()
    {
        _pairs = new Dictionary<string, string>();
    }

    public void Add(string key, string value)
    {
        _pairs[key] = value;
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

    public Dictionary<string, string>.KeyCollection GetAllKeys()
    {
        Dictionary<string, string>.KeyCollection keyColl = _pairs.Keys;
        return keyColl;
    }

    public Dictionary<string, string> GetAllData() 
    {
        return _pairs;
    }
}
