// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.ViewModels;

/// <summary>
/// The asset preview panel: it previews the asset selected alone in the asset view (or the active asset of an editor) with
/// the <see cref="IAssetPreviewService"/> of the session, and shows the view the service builds.
/// </summary>
/// <remarks>
/// It lives as long as the session and owns the preview service, which runs the preview game: the view only binds to it, so
/// hiding the panel pauses the rendering instead of tearing the game down.
/// </remarks>
internal sealed class PreviewViewModel : DispatcherViewModel, IDisposable
{
    private readonly SessionViewModel session;
    private IAssetPreviewService? previewService;
    private object? previewObject;
    private bool isPreviewVisible = true;
    private bool isDisposed;

    public PreviewViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        this.session = session;
        session.AssetCollection.SelectedAssets.CollectionChanged += SelectedAssetsCollectionChanged;
        session.ActiveAssetsChanged += ActiveAssetsChanged;
    }

    /// <summary>
    /// Gets the view of the current preview, or <c>null</c> when there is nothing to preview.
    /// </summary>
    public object? PreviewObject
    {
        get => previewObject;
        private set => SetValue(ref previewObject, value);
    }

    /// <summary>
    /// Gets or sets whether the preview panel is on screen: the preview game renders only then.
    /// </summary>
    public bool IsPreviewVisible
    {
        get => isPreviewVisible;
        set
        {
            if (!SetValue(ref isPreviewVisible, value) || isDisposed)
                return;

            if (value)
                PreviewService?.OnShowPreview();
            else
                PreviewService?.OnHidePreview();
        }
    }

    /// <summary>
    /// Gets the preview service of the session, once a plugin has registered it (none with <c>/DisablePreview</c>).
    /// </summary>
    private IAssetPreviewService? PreviewService
    {
        get
        {
            if (previewService is not null)
                return previewService;

            previewService = ServiceProvider.TryGet<IAssetPreviewService>();
            if (previewService is not null)
                previewService.PreviewAssetUpdated += PreviewAssetUpdated;
            return previewService;
        }
    }

    /// <summary>
    /// Stops listening to the session and disposes the preview service, which exits the preview game.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
            return;
        isDisposed = true;

        session.AssetCollection.SelectedAssets.CollectionChanged -= SelectedAssetsCollectionChanged;
        session.ActiveAssetsChanged -= ActiveAssetsChanged;
        if (previewService is not null)
        {
            previewService.PreviewAssetUpdated -= PreviewAssetUpdated;
            previewService.Dispose();
        }
        PreviewObject = null;
    }

    private void SetAssetToPreview(IReadOnlyCollection<AssetViewModel> assets)
    {
        if (isDisposed)
            return;

        // A multiple selection has no preview
        PreviewService?.SetAssetToPreview(assets.Count == 1 ? assets.First() : null!);
    }

    private void SelectedAssetsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SetAssetToPreview(session.AssetCollection.SelectedAssets);
    }

    private void ActiveAssetsChanged(object? sender, ActiveAssetsChangedArgs e)
    {
        SetAssetToPreview(e.Assets);
    }

    private void PreviewAssetUpdated(object? sender, EventArgs e)
    {
        // Raised on the thread of the preview game: never block it on the UI thread, which may be waiting for the game
        Dispatcher.InvokeAsync(() =>
        {
            if (!isDisposed)
                PreviewObject = previewService?.GetCurrentPreviewView();
        });
    }
}
