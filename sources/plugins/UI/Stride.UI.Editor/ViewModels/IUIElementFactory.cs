// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.UI.Assets;
using Stride.UI;

namespace Stride.UI.Editor.ViewModels
{
    public interface IUIElementFactory
    {
        string Category { get; }

        string Name { get; }

        AssetCompositeHierarchyData<UIElementDesign, UIElement> Create(UIAssetBase targetAsset);
    }
}
