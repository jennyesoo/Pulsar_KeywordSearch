using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal interface IKeywordSearchDataReader
{
    Task<CommonDataModel> GetDataAsync(int id);

    Task<IEnumerable<CommonDataModel>> GetDataAsync();
}
