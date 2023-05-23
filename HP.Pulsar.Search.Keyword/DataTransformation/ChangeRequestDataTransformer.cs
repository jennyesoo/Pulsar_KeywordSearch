using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class ChangeRequestDataTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "DateSubmitter", "DateClosed", "TargetApprovalDate", "RTPDate", "RASDiscoDate" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> changeRequests)
    {
        foreach (CommonDataModel dcr in changeRequests)
        {
            foreach (string key in dcr.GetKeys())
            {
                dcr.Add(key, CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, dcr.GetValue(key), key));
            }
        }

        return changeRequests;
    }
}
