using ConcurrentCollections;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

public class ElementKeyContainer
{
    private ConcurrentHashSet<string> _hashSet;

    public ElementKeyContainer()
    {
        _hashSet = new ConcurrentHashSet<string>();
    }

    public void Add(IEnumerable<string> hashSet)
    {
        if (hashSet == null)
        {
            throw new ArgumentException("hashSet not found");
        }

        if (hashSet?.Any() != true)
        {
            return;
        }

        foreach (string item in hashSet)
        {
            if (!_hashSet.Contains(item)
                && !item.Equals("Id", StringComparison.OrdinalIgnoreCase)
                && !item.Equals("Target", StringComparison.OrdinalIgnoreCase))
            {
                _hashSet.Add(item);
            }
        }
    }

    public IReadOnlyCollection<string> Get()
    {
        return _hashSet;
    }
}
