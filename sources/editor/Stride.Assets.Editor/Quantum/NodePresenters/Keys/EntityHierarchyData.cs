// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Keys;

internal static class EntityHierarchyData
{
    public const string EntityComponentAvailableTypes = nameof(EntityComponentAvailableTypes);
    public static readonly PropertyKey<IEnumerable<AbstractNodeType>> EntityComponentAvailableTypesKey = new(EntityComponentAvailableTypes, typeof(EntityHierarchyData), new PropertyCombinerMetadata(AbstractNodeEntryData.CombineProperty));

    public const string EntityComponentAvailableTypeGroups = nameof(EntityComponentAvailableTypeGroups);
    public static readonly PropertyKey<IEnumerable<AbstractNodeTypeGroup>> EntityComponentAvailableTypeGroupsKey = new(EntityComponentAvailableTypeGroups, typeof(EntityHierarchyData), new PropertyCombinerMetadata(AbstractNodeEntryData.CombineProperty));
}
