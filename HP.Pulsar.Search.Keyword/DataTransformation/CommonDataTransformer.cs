using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using LemmaSharp.Classes;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class CommonDataTransformer
{

    public static string DataProcessingInitializationCombination(List<string> datePropertyList, string propertyValue, string propertyName)
    {
        if (datePropertyList.Contains(propertyName.ToLower()))
        {
            propertyValue = ChangeDateFormat(propertyValue);
        }
        return propertyValue;
    }


    private static string ChangeDateFormat(string propertyValue)
    {
        DateTime dateValue;
        if (DateTime.TryParse(propertyValue, out dateValue))
        {
            return dateValue.ToString("yyyy/MM/dd");
        }
        else
        {
            return propertyValue;
        }
    }

}
