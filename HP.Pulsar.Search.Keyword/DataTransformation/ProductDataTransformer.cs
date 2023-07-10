using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ProductDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "End Of Sales date", "created date", "latest update date", "End Of Production Date", "End Of Service Date", "Last SCM Publish Date" };
    private static readonly List<string> _userNamePropertyList = new();

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> products)
    {
        if (!products.Any())
        {
            return products;
        }

        foreach (CommonDataModel product in products)
        {
            foreach (string key in product.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, product.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, product.GetValue(key)))
                {
                    product.Add(key, propertyValue);
                }
                else if (string.IsNullOrWhiteSpace(propertyValue))
                {
                    product.Delete(key);
                }
            }
        }

        return products;
    }

    public CommonDataModel Transform(CommonDataModel product)
    {
        if (!product.GetElements().Any())
        {
            return product;
        }

        foreach (string key in product.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, product.GetValue(key), key);

            if (!string.IsNullOrWhiteSpace(propertyValue)
                && !string.Equals(propertyValue, product.GetValue(key)))
            {
                product.Add(key, propertyValue);
            }
            else if (string.IsNullOrWhiteSpace(propertyValue))
            {
                product.Delete(key);
            }
        }
        return product;
    }
}
