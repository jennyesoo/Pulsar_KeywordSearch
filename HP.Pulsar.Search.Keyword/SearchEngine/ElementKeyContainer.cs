using ConcurrentCollections;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

public static class ElementKeyContainer
{
    private static ConcurrentHashSet<string> _hashSet;

    static ElementKeyContainer()
    {
        _hashSet = new ConcurrentHashSet<string>();
    }

    public static void Add(IEnumerable<string> hashSet)
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
            if (!_hashSet.Contains(item))
            {
                _hashSet.Add(item);
            }
        }
    }

    public static ISet<string> Get()
    {
        HashSet<string> hashSet = new();

        foreach (string item in _hashSet)
        {
            hashSet.Add(item);
        }

        return hashSet;
    }
}
