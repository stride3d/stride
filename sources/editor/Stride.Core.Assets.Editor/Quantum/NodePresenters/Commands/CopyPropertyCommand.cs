// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

public sealed class CopyPropertyCommand : NodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "CopyProperty";

    /// <inheritdoc />
    public override string Name => CommandName;

    /// <inheritdoc />
    public override CombineMode CombineMode => CombineMode.DoNotCombine;

    /// <inheritdoc />
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        var assetPropertyProvider = nodePresenter.PropertyProvider as IAssetPropertyProviderViewModel;
        return nodePresenter is IAssetNodePresenter & assetPropertyProvider?.RelatedAsset.ServiceProvider.TryGet<ICopyPasteService>() != null;
    }

    /// <inheritdoc />
    public override async Task Execute(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        try
        {
            var assetNodePresenter = (IAssetNodePresenter)nodePresenter;
            var asset = assetNodePresenter.Asset;
            var service = asset?.ServiceProvider.Get<ICopyPasteService>();
            var text = service?.CopyFromAsset(asset?.PropertyGraph, asset?.Id, nodePresenter.Value, assetNodePresenter.IsObjectReference(nodePresenter.Value));
            if (string.IsNullOrEmpty(text)) return;
            if (asset?.ServiceProvider.Get<IClipboardService>().SetTextAsync(text) is Task t) await t;
        }
        catch (AggregateException e) when (e.InnerException is SystemException)
        {
            // We don't provide feedback when copying fails.
            e.Ignore();
        }
        catch (SystemException e)
        {
            // We don't provide feedback when copying fails.
            e.Ignore();
        }
    }
}
