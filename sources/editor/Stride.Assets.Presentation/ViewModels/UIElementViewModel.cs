// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.UI;
using Stride.Core;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class UIElementViewModel : UIHierarchyItemViewModel, IPartDesignViewModel<UIElementDesign, UIElement>, IAssetPropertyProviderViewModel
{
    private string? name;

    public UIElementViewModel(UIBaseViewModel asset, UIElementDesign elementDesign)
        : base(asset, GetOrCreateChildPartDesigns((UIAssetBase)asset.Asset, elementDesign))
    {
        UIElementDesign = elementDesign;
        ElementType = AssetSideUIElement.GetType();
    }

    public UIElement AssetSideUIElement => UIElementDesign.UIElement;

    public Type ElementType { get; }

    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, AssetSideUIElement.Id);

    /// <inheritdoc/>
    public override string? Name
    {
        get => name;
        set => SetValue(ref name, value);
    }

    /// <inheritdoc />
    public override GraphNodePath GetNodePath()
    {
        var node = new GraphNodePath(GetNode());
        node.PushMember(nameof(UIAsset.Hierarchy));
        node.PushTarget();
        node.PushMember(nameof(UIAsset.Hierarchy.Parts));
        node.PushTarget();
        node.PushIndex(new NodeIndex(Id.ObjectId));
        node.PushMember(nameof(UIElementDesign.UIElement));
        node.PushTarget();
        return node;
    }

    internal UIElementDesign UIElementDesign { get; }

    AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

    UIElementDesign IPartDesignViewModel<UIElementDesign, UIElement>.PartDesign => UIElementDesign;

    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

    private static IEnumerable<UIElementDesign> GetOrCreateChildPartDesigns(UIAssetBase asset, UIElementDesign elementDesign)
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

    GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
    {
        return GetNodePath();
    }

    IObjectNode IPropertyProviderViewModel.GetRootNode()
    {
        return Asset.Session.AssetNodeContainer.GetOrCreateNode(AssetSideUIElement);
    }

    bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member)
    {
        if (typeof(PropertyContainerClass).IsAssignableFrom(member.Type))
        {
            // Do not show property container and attached properties in the property grid.
            // Note: when relevant those properties will be available through virtual nodes.
            return false;
        }
        var assetPropertyProvider = (IPropertyProviderViewModel)Asset;
        return assetPropertyProvider.ShouldConstructMember(member);
    }

    bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Asset).ShouldConstructItem(collection, index);
}
