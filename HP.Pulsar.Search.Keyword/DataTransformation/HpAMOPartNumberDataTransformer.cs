using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

internal class HpAMOPartNumberDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "RTP/MR Date",
                                                                     "Select Availability (SA) Date",
                                                                     "General Availability (GA) Date",
                                                                     "End of Manufacturing (EM) Date",
                                                                     "Global Series Planned End Date",
                                                                     "End of Sales (ES) Date" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> hpPartNumber)
    {
        if (!hpPartNumber.Any())
        {
            return null;
        }

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

    public CommonDataModel Transform(CommonDataModel partNumber)
    {
        if (!partNumber.GetElements().Any())
        {
            return null;
        }

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
        return partNumber;
    }
}
