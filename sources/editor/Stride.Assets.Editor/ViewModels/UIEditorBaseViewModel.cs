// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.UI;

namespace Stride.Assets.Editor.ViewModels;

public abstract class UIEditorBaseViewModel : AssetCompositeHierarchyEditorViewModel<UIElementDesign, UIElement, UIBaseViewModel, UIHierarchyItemViewModel>
{
    protected UIEditorBaseViewModel(UIBaseViewModel asset)
        : base(asset)
    {
    }

    public UIRootViewModel HierarchyRoot => (UIRootViewModel)RootPart;
}
