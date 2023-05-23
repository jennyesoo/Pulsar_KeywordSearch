using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class FeatureDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "created", "updated" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> features)
    {
        foreach (CommonDataModel feature in features)
        {
            foreach (string key in feature.GetKeys())
            {
                string propertyValue = feature.GetValue(key);

                if (string.Equals(key, "UpdatedBy")
                    && string.Equals(propertyValue, "dbo"))
                {
                    feature.Delete(key);
                }
                else
                {
                    feature.Add(key, CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, feature.GetValue(key), key));
                }
            }
        }

        return features;
    }
}
