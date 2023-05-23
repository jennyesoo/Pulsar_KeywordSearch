using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ProductDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "servicelifedate", "createddate", "latestupdatedate", "endofproductiondate" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> products)
    {
        foreach (CommonDataModel product in products)
        {
            foreach (string key in product.GetKeys())
            {
                string propertyValue = product.GetValue(key);

                if (string.Equals(key, "CreatorName", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(propertyValue, "dbo", StringComparison.OrdinalIgnoreCase))
                {
                    product.Delete(key);
                }
                else if (string.Equals(key, "LastUpdaterName", StringComparison.OrdinalIgnoreCase)
                         && string.Equals(propertyValue, "dbo", StringComparison.OrdinalIgnoreCase))
                {
                    product.Delete(key);
                }
                else
                {
                    product.Add(key, CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, propertyValue, key));
                }
            }
        }

        return products;
    }
}
