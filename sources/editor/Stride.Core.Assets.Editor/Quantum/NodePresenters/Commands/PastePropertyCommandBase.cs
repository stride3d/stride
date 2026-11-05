// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Collections;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;

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
        ArgumentNullException.ThrowIfNull(nodePresenters);

        foreach (var nodePresenter in nodePresenters)
        {
            var assetNodePresenter = nodePresenter as IAssetNodePresenter;
            var copyPasteService = assetNodePresenter?.Asset?.ServiceProvider.TryGet<ICopyPasteService>();
            if (copyPasteService == null)
                return false;

            var asset = assetNodePresenter!.Asset!.Asset;

            var clipboard = assetNodePresenter?.Asset?.ServiceProvider.TryGet<IClipboardService>();
            if (clipboard == null)
                return false;

            // Some clipboards answer through the UI thread's event loop (X11, when another program owns the clipboard): waiting for
            // them here, on the UI thread, would never end. The content is checked only when the clipboard answers at once (always
            // in WPF); otherwise the paste checks it.
            var text = clipboard.GetTextAsync();
            if (text.IsCompleted && !(text.IsCompletedSuccessfully && copyPasteService.CanPaste(text.Result, asset.GetType(), GetTargetType(nodePresenter))))
                return false;

            // Cannot paste into read-only collection
            if (IsInReadOnlyCollection(nodePresenter) || IsInReadOnlyCollection((nodePresenter as ItemNodePresenter)?.OwnerCollection))
                return false;

            // Cannot paste into read-only property (non-collection)
            if (!nodePresenter.IsEnumerable && nodePresenter.IsReadOnly)
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
        var targetType = GetTargetType(nodePresenter);
        // The command can be enabled before the content of the clipboard is known (see CanPaste)
        if (!copyPasteService.CanPaste(text, asset.Asset.GetType(), targetType))
            return;

        var result = copyPasteService.DeserializeCopiedData(text, asset.Asset, targetType);
        if (result.Items.Count == 0)
            return;

        var nodeAccessor = nodePresenter.GetNodeAccessor();
        var targetNode = nodeAccessor.Node;
        // If the node presenter is a virtual node without node, we cannot paste.
        if (targetNode == null)
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

    private static Type GetTargetType(INodePresenter nodePresenter)
    {
        return (nodePresenter as ItemNodePresenter)?.OwnerCollection.Type ?? nodePresenter.Type;
    }

    private static bool IsInReadOnlyCollection(INodePresenter? nodePresenter)
    {
        if (nodePresenter == null || !nodePresenter.IsEnumerable)
            return false;

        var memberCollection = (nodePresenter as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                               ?? nodePresenter.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
        return memberCollection != null && memberCollection.ReadOnly;
    }
}
