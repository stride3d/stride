// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Assets.Presentation.NodePresenters.Keys
{
    public static class EntityHierarchyData
    {
        public const string EntityComponentAvailableTypes = nameof(EntityComponentAvailableTypes);
        public static readonly PropertyKey<IEnumerable<AbstractNodeType>> EntityComponentAvailableTypesKey = new PropertyKey<IEnumerable<AbstractNodeType>>(EntityComponentAvailableTypes, typeof(EntityHierarchyData), new PropertyCombinerMetadata(AbstractNodeEntryData.CombineProperty));


        public const string EntityComponentAvailableTypeGroups = nameof(EntityComponentAvailableTypeGroups);
        public static readonly PropertyKey<IEnumerable<AbstractNodeTypeGroup>> EntityComponentAvailableTypeGroupsKey = new PropertyKey<IEnumerable<AbstractNodeTypeGroup>>(EntityComponentAvailableTypeGroups, typeof(EntityHierarchyData), new PropertyCombinerMetadata(AbstractNodeEntryData.CombineProperty));
    }
}
