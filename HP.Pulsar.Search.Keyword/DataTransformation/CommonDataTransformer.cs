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

        if (datePropertyList.Contains(propertyName, StringComparer.OrdinalIgnoreCase)
            && TryParseDate(propertyValue, out DateTime date))
        {
            propertyValue = date.ToString("yyyy/MM/dd");
        }

        propertyValue = ReviewNoiseValueAndRemove(propertyValue, propertyName);

        return propertyValue;
    }

    public static bool TryParseDate(string propertyValue, out DateTime date)
    {
        if (DateTime.TryParse(propertyValue, out DateTime dateValue))
        {
            date = dateValue;
            return true;
        }

        date = default;
        return false;
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
