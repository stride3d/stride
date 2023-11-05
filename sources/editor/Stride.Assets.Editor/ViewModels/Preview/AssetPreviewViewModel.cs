// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModels;

namespace Stride.Assets.Editor.ViewModels.Preview;

/// <summary>
/// Base implementation of <see cref="IAssetPreviewViewModel"/>.
/// </summary>
public abstract class AssetPreviewViewModel<TPreview> : DispatcherViewModel, IAssetPreviewViewModel
    where TPreview : IAssetPreview
{
    public SessionViewModel Session { get; }

    protected AssetPreviewViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;
    }

    /// <inheritdoc/>
    public void AttachPreview(IAssetPreview preview)
    {
        OnAttachPreview((TPreview)preview);
    }

    protected abstract void OnAttachPreview(TPreview preview);
}
