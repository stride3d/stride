// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets;
using Xenko.Assets.UI;
using Xenko.UI;

namespace Xenko.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    public interface IUIElementFactory
    {
        string Category { get; }

        string Name { get; }

        AssetCompositeHierarchyData<UIElementDesign, UIElement> Create(UIAssetBase targetAsset);
    }
}
