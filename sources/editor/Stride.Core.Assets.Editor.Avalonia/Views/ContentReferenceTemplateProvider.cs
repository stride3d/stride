// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class ContentReferenceTemplateProvider : NodeViewModelTemplateProvider
{
    public bool DynamicThumbnail { get; set; }

    public override string Name => $"{(DynamicThumbnail ? "Thumbnail" : "Simple")}Reference";

    public override bool MatchNode(NodeViewModel node)
    {
        if (!AssetRegistry.CanBeAssignedToContentTypes(node.Type, checkIsUrlType: true))
            return false;

        node.AssociatedData.TryGetValue("DynamicThumbnail", out var hasDynamic);
        return (bool)(hasDynamic ?? false) == DynamicThumbnail;
    }
}
