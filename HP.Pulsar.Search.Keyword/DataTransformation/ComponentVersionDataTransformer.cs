using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ComponentVersionDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "Intro Date", "MassProduction", "End Of Life Date", "Samples Available" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> componentVersions)
    {
        if (!componentVersions.Any())
        {
            return null;
        }

        foreach (CommonDataModel version in componentVersions)
        {
            foreach (string key in version.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, version.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, version.GetValue(key)))
                {
                    version.Add(key, propertyValue);
                }
                else if (string.IsNullOrWhiteSpace(propertyValue))
                {
                    version.Delete(key);
                }
            }
        }

        return componentVersions;
    }

    public CommonDataModel Transform(CommonDataModel version)
    {
        if (!version.GetElements().Any())
        {
            return null;
        }

        foreach (string key in version.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, version.GetValue(key), key);

            if (!string.IsNullOrWhiteSpace(propertyValue)
                && !string.Equals(propertyValue, version.GetValue(key)))
            {
                version.Add(key, propertyValue);
            }
            else if (string.IsNullOrWhiteSpace(propertyValue))
            {
                version.Delete(key);
            }
        }

        return version;
    }
}
