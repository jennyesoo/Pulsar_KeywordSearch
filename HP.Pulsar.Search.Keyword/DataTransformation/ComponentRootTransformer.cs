﻿using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

internal class ComponentRootTransformer : IDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "created", "Deleted", "updated" };
    private static readonly List<string> _userNamePropertyList = new() { "Deleted by", "Updated by", "Created by" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> componentRoots)
    {
        if (!componentRoots.Any())
        {
            return componentRoots;
        }

        foreach (CommonDataModel root in componentRoots)
        {
            foreach (string key in root.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, root.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, root.GetValue(key)))
                {
                    root.Add(key, propertyValue);
                }
                else if (string.IsNullOrWhiteSpace(propertyValue))
                {
                    root.Delete(key);
                }
            }
        }

        return componentRoots;
    }

    public CommonDataModel Transform(CommonDataModel componentRoot)
    {
        if (!componentRoot.GetElements().Any())
        {
            return componentRoot;
        }

        foreach (string key in componentRoot.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, componentRoot.GetValue(key), key);

            if (!string.IsNullOrWhiteSpace(propertyValue)
                && !string.Equals(propertyValue, componentRoot.GetValue(key)))
            {
                componentRoot.Add(key, propertyValue);
            }
            else if (string.IsNullOrWhiteSpace(propertyValue))
            {
                componentRoot.Delete(key);
            }
        }
        return componentRoot;
    }
}
