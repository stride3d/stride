// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Assets.UI;
using Stride.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    public interface IUIElementFactory
    {
        string Category { get; }

        string Name { get; }

        AssetCompositeHierarchyData<UIElementDesign, UIElement> Create(UIAssetBase targetAsset);
    }
}
