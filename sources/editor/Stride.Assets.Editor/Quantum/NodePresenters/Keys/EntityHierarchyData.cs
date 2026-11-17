// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System.Collections.Generic;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Keys;

internal static class EntityHierarchyData
{
    public const string EntityComponentAvailableTypes = nameof(EntityComponentAvailableTypes);
    public static readonly PropertyKey<IEnumerable<AbstractNodeType>> EntityComponentAvailableTypesKey = new(EntityComponentAvailableTypes, typeof(EntityHierarchyData), new PropertyCombinerMetadata(AbstractNodeEntryData.CombineProperties<AbstractNodeType>));

    public const string EntityComponentAvailableTypeGroups = nameof(EntityComponentAvailableTypeGroups);
    public static readonly PropertyKey<IEnumerable<AbstractNodeTypeGroup>> EntityComponentAvailableTypeGroupsKey = new(EntityComponentAvailableTypeGroups, typeof(EntityHierarchyData), new PropertyCombinerMetadata(CombineTypeGroups));

    /// <summary>
    /// Combines the component categories of several entities: each category keeps the component types that every entity can
    /// take, and a category with none left is dropped.
    /// </summary>
    /// <param name="properties">The categories of each entity, each an <see cref="IEnumerable{AbstractNodeTypeGroup}"/>.</param>
    /// <returns>The categories in the order of the first entity, with their types in the same order.</returns>
    public static IEnumerable<AbstractNodeTypeGroup> CombineTypeGroups(IEnumerable<object> properties)
    {
        var groupsOfEach = properties.Cast<IEnumerable<AbstractNodeTypeGroup>>().Select(x => x.ToList()).ToList();
        if (groupsOfEach.Count == 0)
            return [];

        var commonTypes = new HashSet<AbstractNodeType>(groupsOfEach[0].SelectMany(x => x.Types));
        foreach (var groups in groupsOfEach.Skip(1))
            commonTypes.IntersectWith(groups.SelectMany(x => x.Types));

        var result = new List<AbstractNodeTypeGroup>();
        foreach (var group in groupsOfEach[0])
        {
            var types = group.Types.Where(commonTypes.Contains).ToList();
            if (types.Count > 0)
                result.Add(types.Count == group.Types.Count() ? group : new AbstractNodeTypeGroup(group.GroupName, types));
        }
        return result;
    }
}
