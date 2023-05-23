using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ProductDropDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "CreatedDate", "UpdatedDate", "UpcomingRTMDate" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> productDrop)
    {
        foreach (CommonDataModel dcr in productDrop)
        {
            foreach (string key in dcr.GetKeys())
            {
                dcr.Add(key, CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, dcr.GetValue(key), key));
            }
        }

        return productDrop;
    }
}
