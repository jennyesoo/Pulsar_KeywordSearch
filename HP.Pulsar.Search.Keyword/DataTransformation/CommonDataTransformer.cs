using Meilisearch;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public static class CommonDataTransformer
{
    public static string DataProcessingInitializationCombination(List<string> datePropertyList, string propertyValue, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyValue)
            || string.IsNullOrWhiteSpace(propertyName)
            || datePropertyList == null)
        {
            return propertyValue;
        }

        if (datePropertyList.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
        {
            propertyValue = ChangeDateFormat(propertyValue);
        }
        
        propertyValue = ReviewNoiseValueAndRemove(propertyValue, propertyName);

        return propertyValue;
    }

    private static string ChangeDateFormat(string propertyValue)
    {
        if (DateTime.TryParse(propertyValue, out DateTime dateValue))
        {
            return dateValue.ToString("yyyy/MM/dd");
        }
        return propertyValue;
    }

    private static string ReviewNoiseValueAndRemove(string propertyValue, string propertyName)
    {
        if (string.Equals(propertyValue, "None", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyValue, "N/A", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyValue, "dbo", StringComparison.OrdinalIgnoreCase))
        {
            propertyValue = null;
        }

        if (propertyName.Contains("email", StringComparison.OrdinalIgnoreCase)
            && propertyValue.Contains("disabled-", StringComparison.OrdinalIgnoreCase))
        {
            propertyValue = null;
        }

        return propertyValue;
    }
}
