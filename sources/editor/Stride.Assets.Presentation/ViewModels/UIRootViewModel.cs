// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.UI;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class UIRootViewModel : UIHierarchyItemViewModel
{
    protected UIRootViewModel(UIBaseViewModel asset, IEnumerable<UIElementDesign> rootElements)
        : base(asset, rootElements)
    {
    }

    /// <inheritdoc/>
    public override GraphNodePath GetNodePath()
    {
        var path = new GraphNodePath(GetNode());
        path.PushMember(nameof(UIAsset.Hierarchy));
        path.PushTarget();
        return path;
    }
}
