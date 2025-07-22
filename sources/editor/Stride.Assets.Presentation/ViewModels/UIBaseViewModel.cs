// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.UI;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.UI;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// Base view model for <see cref="UIAssetBase"/>.
/// </summary>
public abstract class UIBaseViewModel : AssetCompositeHierarchyViewModel<UIElementDesign, UIElement>
{
    protected UIBaseViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    public override UIElementViewModel CreatePartViewModel(UIElementDesign elementDesign)
    {
        return new UIElementViewModel(this, elementDesign);
    }

    /// <inheritdoc />
    protected override GraphNodePath GetPathToPropertiesRootNode()
    {
        var path = base.GetPathToPropertiesRootNode();
        path.PushMember(nameof(UIAssetBase.Design));
        path.PushTarget();
        return path;
    }

    /// <inheritdoc />
    protected override IObjectNode? GetPropertiesRootNode()
    {
        return Session.AssetNodeContainer.GetNode(((UIAssetBase)Asset).Design);
    }
}
