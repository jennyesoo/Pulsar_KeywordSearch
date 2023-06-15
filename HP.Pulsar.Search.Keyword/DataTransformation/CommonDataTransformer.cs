namespace HP.Pulsar.Search.Keyword.DataTransformation;

public static class CommonDataTransformer
{
    public static string DataProcessingInitializationCombination(List<string> datePropertyList, List<string> userNamePropertyList, string propertyValue, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyValue)
            || string.IsNullOrWhiteSpace(propertyName))
        {
            return propertyValue;
        }

        AdjustDate(datePropertyList, ref propertyValue, propertyName);
        AdjustUserName(userNamePropertyList, ref propertyValue, propertyName);
        ReviewNoiseValueAndRemove(ref propertyValue, propertyName);

        return propertyValue;
    }

    private static string AdjustDate(List<string> datePropertyList, ref string propertyValue, string propertyName)
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

        return propertyValue;
    }


    private static string AdjustUserName(List<string> userNamePropertyList, ref string propertyValue, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyValue)
                || string.IsNullOrWhiteSpace(propertyName)
                || !userNamePropertyList.Any())
        {
            return propertyValue;
        }

        if (userNamePropertyList.Any(element => propertyName.IndexOf(element, StringComparison.OrdinalIgnoreCase) >= 0)
            && propertyValue.Contains(","))
        {
            string[] temp = propertyValue.Split(',');
            
            if (temp.Length != 2) 
            {
                return propertyValue;
            }

            propertyValue = temp[1].Trim() + " " + temp[0].Trim();
        }

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

    private static string ReviewNoiseValueAndRemove(ref string propertyValue, string propertyName)
    {
        if (string.Equals(propertyValue, "None", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyValue, "N/A", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyValue, "dbo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyValue, ".", StringComparison.OrdinalIgnoreCase))
        {
            propertyValue = string.Empty;
            return propertyValue;
        }

        if (propertyName.IndexOf("email", StringComparison.OrdinalIgnoreCase) >= 0
            && propertyValue.IndexOf("disabled-", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            propertyValue = propertyValue.Split(new char[] { '-' }).Last();
        }

        return propertyValue;
    }
}
