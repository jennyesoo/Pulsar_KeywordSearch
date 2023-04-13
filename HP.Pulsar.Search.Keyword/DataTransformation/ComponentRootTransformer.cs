using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation
{
    internal class ComponentRootTransformer : IDataTransformer
    {
        private static readonly List<string> _datePropertyList = new() { "created" , "muiawareDate" , "updated" };

        public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> componentRoots)
        {
            foreach (CommonDataModel root in componentRoots)
            {
                foreach (string key in root.GetKeys())
                {
                    root.Add(key, CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, root.GetValue(key), key));
                }
            }
            return componentRoots;
        }
    }
}
