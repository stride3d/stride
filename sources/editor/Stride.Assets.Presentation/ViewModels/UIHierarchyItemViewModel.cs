// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.UI;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class UIHierarchyItemViewModel : AssetCompositeItemViewModel<UIBaseViewModel, UIHierarchyItemViewModel>
{
    protected UIHierarchyItemViewModel(UIBaseViewModel asset, IEnumerable<UIElementDesign> childElements)
        : base(asset)
    {
        AddItems(childElements.Select(asset.CreatePartViewModel));
    }

    protected UIAssetBase UIAsset => (UIAssetBase)Asset.Asset;
}
