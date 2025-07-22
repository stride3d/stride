// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

internal sealed class FetchAssetCommand : NodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "FetchAsset";
    /// <summary>
    /// The current session.
    /// </summary>
    private readonly SessionViewModel session;

    /// <summary>
    /// Initializes a new instance of the <see cref="FetchAssetCommand"/> class.
    /// </summary>
    /// <param name="session">The current session.</param>
    public FetchAssetCommand(SessionViewModel session)
    {
        this.session = session;
    }

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override CombineMode CombineMode => CombineMode.AlwaysCombine;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        if (nodePresenter.Descriptor?.GetInnerCollectionType() is { } type)
            return AssetRegistry.CanBeAssignedToContentTypes(type, checkIsUrlType: true);

        return false;
    }

    /// <inheritdoc/>
    public override Task Execute(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        return Fetch(session, nodePresenter.Value);
    }

    /// <summary>
    /// Fetches the entity corresponding to the given content.
    /// </summary>
    /// <param name="session">The current session.</param>
    /// <param name="content">The proxy object corresponding to the asset to fetch.</param>
    public static async Task Fetch(SessionViewModel session, object content)
    {
        var asset = ContentReferenceHelper.GetReferenceTarget(session, content);
        if (asset != null)
        {
            await session.Dispatcher.InvokeAsync(() => session.AssetCollection.SelectAssetCommand.Execute(asset));
        }
    }
}
