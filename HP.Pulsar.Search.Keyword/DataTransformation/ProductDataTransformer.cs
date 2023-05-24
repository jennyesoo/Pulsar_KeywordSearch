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
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, product.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, product.GetValue(key)))
                {
                    product.Add(key, propertyValue);
                }
                else if(string.IsNullOrWhiteSpace(propertyValue))
                {
                    product.Delete(key);
                }
            }
        }

        return products;
    }
}
