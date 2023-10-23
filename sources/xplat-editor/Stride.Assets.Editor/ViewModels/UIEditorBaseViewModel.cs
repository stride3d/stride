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

    /// <inheritdoc />
    protected override Task RefreshEditorProperties()
    {
        // note: here we are assuming that all items are UIElementViewModel.
        //       if that were to change, revisit this code.
        EditorProperties.UpdateTypeAndName(SelectedItems.OfType<UIElementViewModel>(), SelectedItems.Count, e => e.ElementType.Name, e => e.AssetSideUIElement.Name, "elements");
        return EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems.OfType<UIElementViewModel>());
    }
}
