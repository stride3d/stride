// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyRootViewModel : EntityHierarchyItemViewModel
{
    protected EntityHierarchyRootViewModel(EntityHierarchyViewModel asset)
        : base(asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns())
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

    /// <inheritdoc/>
    public override GraphNodePath GetNodePath()
    {
        var path = new GraphNodePath(GetNode());
        path.PushMember(nameof(EntityHierarchy.Hierarchy));
        path.PushTarget();
        return path;
    }
}
