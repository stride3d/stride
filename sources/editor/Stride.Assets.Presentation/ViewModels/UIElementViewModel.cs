// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.ViewModels;

public class UIElementViewModel : UIHierarchyItemViewModel
{
    private string? name;

    public UIElementViewModel(UIBaseViewModel asset, UIElementDesign elementDesign)
        : base(asset, GetOrCreateChildPartDesigns((UIAssetBase)asset.Asset, elementDesign))
    {
        UIElementDesign = elementDesign;
    }

    public override string? Name
    {
        get => name;
        set => SetValue(ref name, value);
    }

    internal UIElementDesign UIElementDesign { get; }

    private static IEnumerable<UIElementDesign> GetOrCreateChildPartDesigns( UIAssetBase asset, UIElementDesign elementDesign)
    {
        switch (elementDesign.UIElement)
        {
            case ContentControl control:                
                if (control.Content != null)
                {
                    if (!asset.Hierarchy.Parts.TryGetValue(control.Content.Id, out var partDesign))
                    {
                        partDesign = new UIElementDesign(control.Content);
                    }
                    if (control.Content != partDesign.UIElement) throw new InvalidOperationException();
                    yield return partDesign;
                }
                break;

            case Panel panel:
                foreach (var child in panel.Children)
                {
                    if (!asset.Hierarchy.Parts.TryGetValue(child.Id, out var childDesign))
                    {
                        childDesign = new UIElementDesign(child);
                    }
                    if (child != childDesign.UIElement) throw new InvalidOperationException();
                    yield return childDesign;
                }
                break;
        }
    }
}
