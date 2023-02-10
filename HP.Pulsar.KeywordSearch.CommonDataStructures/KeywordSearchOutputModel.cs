namespace HP.Pulsar.KeywordSearch.CommonDataStructures
{
    public class KeywordSearchOutputModel
    {
        public KeywordSearchOutputModel(SearchType type, IEnumerable<KeyValuePair<string, string>> values)
        {
            Type = type;
            Values = values;
        }

        public SearchType Type { get;}

        public IEnumerable<KeyValuePair<string, string>> Values { get;}
    }
}
