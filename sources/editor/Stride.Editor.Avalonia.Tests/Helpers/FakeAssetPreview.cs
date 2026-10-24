// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// An asset preview that records what the view tells it.
/// </summary>
public sealed class FakeAssetPreview : IAssetPreview
{
    public IAssetPreviewViewModel PreviewViewModel { get; set; } = null!;

    public event EventHandler? ContentLoaded;

    public AssetViewModel AssetViewModel => throw new NotSupportedException("No asset in this fake.");

    public RenderingMode RenderingMode => RenderingMode.LDR;

    /// <summary>
    /// How many times the view reported it was attached.
    /// </summary>
    public int ViewAttachedCount { get; private set; }

    public Task<object> Initialize(AssetViewModel asset, IPreviewBuilder builder) => Task.FromResult<object>(this);

    public Task IsInitialized() => Task.CompletedTask;

    public Task Update() => Task.CompletedTask;

    public Task Dispose() => Task.CompletedTask;

    public void OnViewAttached() => ViewAttachedCount++;

    public void RaiseContentLoaded() => ContentLoaded?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// A preview view model that records the previews attached to it.
/// </summary>
public sealed class FakePreviewViewModel : IAssetPreviewViewModel
{
    public ISessionViewModel Session => throw new NotSupportedException("No session in this fake.");

    public List<IAssetPreview> Attached { get; } = [];

    public void AttachPreview(IAssetPreview preview) => Attached.Add(preview);
}
