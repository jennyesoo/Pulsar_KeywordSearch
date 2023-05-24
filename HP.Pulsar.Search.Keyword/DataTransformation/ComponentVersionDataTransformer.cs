using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ComponentVersionDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "MassProduction", "SampleDate" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> componentVersions)
    {
        foreach (CommonDataModel rootversion in componentVersions)
        {
            foreach (string key in rootversion.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, rootversion.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, rootversion.GetValue(key)))
                {
                    rootversion.Add(key, propertyValue);
                }
                else if (string.IsNullOrWhiteSpace(propertyValue))
                {
                    rootversion.Delete(key);
                }
            }
        }

        return componentVersions;
    }
}
