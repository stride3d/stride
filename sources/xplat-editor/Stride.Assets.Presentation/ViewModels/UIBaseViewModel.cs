// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.UI;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// Base view model for <see cref="UIAssetBase"/>.
/// </summary>
public abstract class UIBaseViewModel : AssetCompositeHierarchyViewModel<UIElementDesign, UIElement>
{
    protected UIBaseViewModel(AssetItem assetItem, DirectoryBaseViewModel directory)
        : base(assetItem, directory)
    {
    }

    /// <inheritdoc />
    public override UIElementViewModel CreatePartViewModel(UIElementDesign elementDesign)
    {
        return new UIElementViewModel(this, elementDesign);
    }
}
