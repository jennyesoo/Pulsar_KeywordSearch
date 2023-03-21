
namespace HP.Pulsar.Search.Keyword.CommonDataStructure
{
    public class KeywordSearchOutputModel
    {
        public KeywordSearchOutputModel(SearchType type, IEnumerable<KeyValuePair<string, string>> values)
        {
            _type = type;
            _values = values;
        }

        public SearchType _type { get; }

        public IEnumerable<KeyValuePair<string, string>> _values { get; }
    }
}
