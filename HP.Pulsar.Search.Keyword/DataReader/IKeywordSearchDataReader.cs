using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal interface IKeywordSearchDataReader
{
    Task<IEnumerable<CommonDataModel>> GetDataAsync();
}
