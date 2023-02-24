using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

internal interface IDataTranformer
{
    IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> models);
}