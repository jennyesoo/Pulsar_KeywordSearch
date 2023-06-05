using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ProductDropDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "CreatedDate", "UpdatedDate", "UpcomingRTMDate" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> productDrop)
    {
        if (!productDrop.Any())
        {
            return null;
        }

        foreach (CommonDataModel dcr in productDrop)
        {
            foreach (string key in dcr.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, dcr.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue))
                {
                    dcr.Add(key, propertyValue);
                }
            }
        }

        return productDrop;
    }

    public CommonDataModel Transform(CommonDataModel productDrop)
    {
        if (!productDrop.GetElements().Any())
        {
            return null;
        }

        foreach (string key in productDrop.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, productDrop.GetValue(key), key);

            if (!string.IsNullOrWhiteSpace(propertyValue))
            {
                productDrop.Add(key, propertyValue);
            }
        }

        return productDrop;
    }
}
