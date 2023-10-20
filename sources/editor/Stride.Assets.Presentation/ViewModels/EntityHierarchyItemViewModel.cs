// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyItemViewModel : AssetCompositeItemViewModel<EntityHierarchyViewModel, EntityHierarchyItemViewModel>
{
    protected EntityHierarchyItemViewModel(EntityHierarchyViewModel asset, IEnumerable<EntityDesign> childEntities)
        : base(asset)
    {
        AddItems(childEntities.Select(asset.CreatePartViewModel));
    }
}
