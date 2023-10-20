// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyRootViewModel : EntityHierarchyItemViewModel
{
    protected EntityHierarchyRootViewModel(EntityHierarchyViewModel asset)
        : base(asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns())
    {
    }
}
