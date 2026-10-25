// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

/// <summary>
/// A preview service that builds a <see cref="Border"/> per previewed asset, synchronously, and records what the editor asks
/// of it: the tests run without the preview game.
/// </summary>
internal sealed class FakeAssetPreviewService : IAssetPreviewService
{
    private object? currentView;

    /// <summary>
    /// The assets the editor asked to preview, in order; <c>null</c> asks for no preview.
    /// </summary>
    public List<AssetViewModel?> Requests { get; } = [];

    public int ShowCount { get; private set; }

    public int HideCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public event EventHandler<EventArgs>? PreviewAssetUpdated;

    public void SetAssetToPreview(AssetViewModel asset)
    {
        Requests.Add(asset);
        currentView = asset is null ? null : new Border { Tag = asset };
        PreviewAssetUpdated?.Invoke(this, EventArgs.Empty);
    }

    public object GetCurrentPreviewView() => currentView!;

    public void OnShowPreview() => ShowCount++;

    public void OnHidePreview() => HideCount++;

    public void Dispose() => IsDisposed = true;
}
