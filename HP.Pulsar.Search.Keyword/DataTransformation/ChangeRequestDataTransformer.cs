using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ChangeRequestDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "Date Submitted", "Date Closed", "Target Approval Date", "RTP Date", "End of Manufacturing Date", "Samples Available Date" };
    private static readonly List<string> _userNamePropertyList = new() { "Approvers" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> changeRequests)
    {
        if (!changeRequests.Any())
        {
            return changeRequests;
        }

        foreach (CommonDataModel dcr in changeRequests)
        {
            foreach (string key in dcr.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, dcr.GetValue(key), key);

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
            return changeRequest;
        }

        foreach (string key in changeRequest.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, changeRequest.GetValue(key), key);

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
