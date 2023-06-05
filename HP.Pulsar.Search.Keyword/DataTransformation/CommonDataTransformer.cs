namespace HP.Pulsar.Search.Keyword.DataTransformation;

public static class CommonDataTransformer
{
    public static string DataProcessingInitializationCombination(List<string> datePropertyList, string propertyValue, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyValue)
            || string.IsNullOrWhiteSpace(propertyName)
            || !datePropertyList.Any())
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
            return string.Empty;
        }

        if (propertyName.IndexOf("email", StringComparison.OrdinalIgnoreCase) >= 0
            && propertyValue.IndexOf("disabled-", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            propertyValue = propertyValue.Split(new char[] { '-' }).Last();
        }

        return propertyValue;
    }
}
