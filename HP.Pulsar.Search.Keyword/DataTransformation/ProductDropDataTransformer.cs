using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ProductDropDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "Created Date", "Updated Date", "Upcoming RTM Date" };
    private static readonly List<string> _userNamePropertyList = new() { "Created by", "Last Updated by" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> productDrop)
    {
        if (!productDrop.Any())
        {
            return productDrop;
        }

        foreach (CommonDataModel dcr in productDrop)
        {
            foreach (string key in dcr.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, dcr.GetValue(key), key);

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
            return productDrop;
        }

        foreach (string key in productDrop.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, productDrop.GetValue(key), key);

            if (!string.IsNullOrWhiteSpace(propertyValue))
            {
                productDrop.Add(key, propertyValue);
            }
        }

        return productDrop;
    }
}
