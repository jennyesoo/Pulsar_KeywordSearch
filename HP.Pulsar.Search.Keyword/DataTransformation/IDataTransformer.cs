using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

internal interface IDataTransformer
{
    IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> models);
    CommonDataModel Transform(CommonDataModel model);

}