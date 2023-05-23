namespace HP.Pulsar.Search.Keyword.CommonDataStructure;

public class SingleOutputModel
{
    public SingleOutputModel(SearchType type,
                             int id,
                             string name,
                             IEnumerable<KeyValuePair<string, string>> values)
    {
        Type = type;
        DataPairs = values;
        Id = id;
        Name = name;
    }

    public SearchType Type { get; }

    public int Id { get; }

    public string Name { get; }

    public IEnumerable<KeyValuePair<string, string>> DataPairs { get; }
}
