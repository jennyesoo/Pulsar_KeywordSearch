using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

internal class HpAMOPartNumberDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "RTPDate", "SADate", "GADate", "EMDate", "GSEOLDate", "ESDate" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> hpPartNumber)
    {
        foreach (CommonDataModel partNumber in hpPartNumber)
        {
            foreach (string key in partNumber.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, partNumber.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, partNumber.GetValue(key)))
                {
                    partNumber.Add(key, propertyValue);
                }
                else if (string.IsNullOrWhiteSpace(propertyValue))
                {
                    partNumber.Delete(key);
                }
            }
        }

        return hpPartNumber;
    }
}
