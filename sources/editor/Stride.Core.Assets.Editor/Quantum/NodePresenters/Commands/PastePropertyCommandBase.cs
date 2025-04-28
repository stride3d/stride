// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

public abstract class PastePropertyCommandBase : NodePresenterCommandBase
{
    /// <inheritdoc />
    public override bool CanExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object? parameter)
    {
        return CanPaste(nodePresenters);
    }

    public override bool CanAttach(INodePresenter nodePresenter)
    {
        // Attach to any node
        return true;
    }

    protected virtual bool CanPaste(IReadOnlyCollection<INodePresenter> nodePresenters)
    {
        foreach (var nodePresenter in nodePresenters)
        {
            var assetNodePresenter = nodePresenter as IAssetNodePresenter;
            var copyPasteService = assetNodePresenter?.Asset?.ServiceProvider.TryGet<ICopyPasteService>();
            if (copyPasteService is null)
                return false;

            var asset = assetNodePresenter!.Asset!.Asset;

            var clipboard = assetNodePresenter?.Asset?.ServiceProvider.TryGet<IClipboardService>();
            if (clipboard is null)
                return false;

            if (!copyPasteService.CanPaste(clipboard.GetTextAsync().Result, asset.GetType(), (nodePresenter as ItemNodePresenter)?.OwnerCollection.Type ?? nodePresenter.Type))
                return false;

            // Cannot paste into read-only collection
            if (IsInReadOnlyCollection(nodePresenter) || IsInReadOnlyCollection((nodePresenter as ItemNodePresenter)?.OwnerCollection))
                return false;

            // Cannot paste into read-only property (non-collection)
            if (nodePresenter is { IsEnumerable: false, IsReadOnly: true })
                return false;
        }
        return true;
    }

    protected async Task DoPasteAsync(INodePresenter nodePresenter, bool replace)
    {
        var asset = ((IAssetNodePresenter)nodePresenter).Asset;
        if (asset is null)
            return;

        var clipboard = asset.ServiceProvider.Get<IClipboardService>();
        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(text))
            return;

        var copyPasteService = asset.ServiceProvider.Get<ICopyPasteService>();
        var result = copyPasteService.DeserializeCopiedData(text, asset.Asset, (nodePresenter as ItemNodePresenter)?.OwnerCollection.Type ?? nodePresenter.Type);
        if (result.Items.Count == 0)
            return;

        var nodeAccessor = nodePresenter.GetNodeAccessor();
        var targetNode = nodeAccessor.Node;
        // If the node presenter is a virtual node without node, we cannot paste.
        if (targetNode is null)
            return;

        var actionService = asset.UndoRedoService;
        using var transaction = actionService.CreateTransaction();

        // FIXME: for now we only handle one result item
        var item = result.Items[0];
        if (item.Data is ICollection && !CollectionDescriptor.IsCollection(targetNode.Type))
            return; // cannot paste a collection to a non-collection content

        var propertyContainer = new PropertyContainer { { AssetPropertyPasteProcessor.IsReplaceKey, replace } };
        await (item.Processor?.Paste(item, asset.PropertyGraph, ref nodeAccessor, ref propertyContainer) ?? Task.CompletedTask);
        actionService.SetName(transaction, replace ? "Replace property" : "Paste property");
    }

    private static bool IsInReadOnlyCollection(INodePresenter? nodePresenter)
    {
        if (nodePresenter is null || !nodePresenter.IsEnumerable)
            return false;

        var memberCollection = (nodePresenter as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                               ?? nodePresenter.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
        return memberCollection is { ReadOnly: true };
    }
}
