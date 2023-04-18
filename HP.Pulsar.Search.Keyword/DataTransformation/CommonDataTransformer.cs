using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using LemmaSharp.Classes;

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

        if (datePropertyList.Contains(propertyName , StringComparer.OrdinalIgnoreCase))
        {
            propertyValue = ChangeDateFormat(propertyValue);
        }
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
}
