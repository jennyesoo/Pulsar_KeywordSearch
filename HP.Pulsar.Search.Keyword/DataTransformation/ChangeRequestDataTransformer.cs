using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ChangeRequestDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "DateSubmitter", "DateClosed", "TargetApprovalDate", "RTPDate", "RASDiscoDate" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> changeRequests)
    {
        if (!changeRequests.Any())
        {
            return null;
        }

        foreach (CommonDataModel dcr in changeRequests)
        {
            foreach (string key in dcr.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, dcr.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, dcr.GetValue(key)))
                {
                    dcr.Add(key, propertyValue);
                }
                else if (string.IsNullOrWhiteSpace(propertyValue))
                {
                    dcr.Delete(key);
                }
            }
        }

        return changeRequests;
    }

    public CommonDataModel Transform(CommonDataModel changeRequest)
    {
        if (!changeRequest.GetElements().Any())
        {
            return null;
        }

        foreach (string key in changeRequest.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, changeRequest.GetValue(key), key);

            if (!string.IsNullOrWhiteSpace(propertyValue)
                && !string.Equals(propertyValue, changeRequest.GetValue(key)))
            {
                changeRequest.Add(key, propertyValue);
            }
            else if (string.IsNullOrWhiteSpace(propertyValue))
            {
                changeRequest.Delete(key);
            }
        }
        return changeRequest;
    }
}
