// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class EditorCollectionViewModel : DispatcherViewModel, IAssetEditorsManager
{
    private AssetEditorViewModel? activeEditor;
    private readonly ObservableList<AssetEditorViewModel> editors = [];
    private readonly Dictionary<AssetViewModel, AssetEditorViewModel> openedAssets = new();

    public EditorCollectionViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;
        ServiceProvider.RegisterService(this);
    }

    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EditorCollectionViewModel));
        ServiceProvider.UnregisterService(this);
        base.Destroy();
    }

    public AssetEditorViewModel? ActiveEditor
    {
        get => activeEditor;
        set => SetValue(ref activeEditor, value);
    }

    public IReadOnlyObservableCollection<AssetEditorViewModel> Editors => editors;

    public SessionViewModel Session { get; }

    /// <inheritdoc/>
    public Task OpenAssetEditorAsync(AssetViewModel asset)
    {
        OpenAssetEditor(asset);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens the editor of the given asset, or makes it the active one when it is open already.
    /// </summary>
    /// <param name="asset">The asset to edit.</param>
    public void OpenAssetEditor(AssetViewModel asset)
    {
        if (openedAssets.TryGetValue(asset, out var editor))
        {
             ActiveEditor = editor;
             return;
        }

        var editorType = ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewModelType(asset.GetType());
        if (editorType is not null)
        {
            editor = (AssetEditorViewModel)Activator.CreateInstance(editorType, asset)!;
            openedAssets.Add(asset, editor);
            editors.Add(editor);
            ActiveEditor = editor;
            // The editor starts what it needs once it is open (e.g. the game of a scene editor)
            InitializeEditorAsync(editor);
        }
    }

    private static async void InitializeEditorAsync(AssetEditorViewModel editor)
    {
        try
        {
            // An editor that could not initialize stays open, with what works
            if (!await editor.Initialize())
                GlobalLogger.GetLogger(nameof(EditorCollectionViewModel)).Warning($"The editor of {editor.Asset.Url} did not initialize completely.");
        }
        catch (Exception e)
        {
            GlobalLogger.GetLogger(nameof(EditorCollectionViewModel)).Error($"The editor of {editor.Asset.Url} could not initialize.", e);
        }
    }

    /// <inheritdoc/>
    /// <remarks>The editor agrees through <see cref="AssetEditorViewModel.PreviewCloseAsync"/>.</remarks>
    public async Task<bool> CloseAssetEditorAsync(AssetViewModel asset, bool? save)
    {
        if (!openedAssets.TryGetValue(asset, out var editor))
            return true;

        // The user sees the editor that asks
        if (save is null)
            ActiveEditor = editor;

        if (!await editor.PreviewCloseAsync(save))
            return false;

        // Closed meanwhile (e.g. by another close while the user answered)
        if (!openedAssets.Remove(asset))
            return true;

        var index = editors.IndexOf(editor);
        editors.Remove(editor);
        if (ActiveEditor == editor)
            ActiveEditor = editors.Count > 0 ? editors[Math.Min(index, editors.Count - 1)] : null;
        editor.Destroy();
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// It stops at the first editor that the user keeps open; the editors closed before it stay closed. Once all are closed, it
    /// waits until they have stopped (<see cref="AssetEditorViewModel.Stopped"/>), as long as <see cref="StopTimeout"/> at most:
    /// the session closes next, and the plugins leave with the settings keys that the games of the editors read on every frame.
    /// </remarks>
    public async Task<bool> CloseAllEditorsAsync(bool? save)
    {
        var closedEditors = new List<AssetEditorViewModel>();
        foreach (var editor in editors.ToList())
        {
            if (!await CloseAssetEditorAsync(editor.Asset, save))
                return false;
            closedEditors.Add(editor);
        }

        await WaitUntilStoppedAsync(closedEditors);
        return true;
    }

    /// <summary>
    /// How long <see cref="CloseAllEditorsAsync"/> waits for the closed editors to stop; a game that hangs does not keep the
    /// editor from closing.
    /// </summary>
    internal TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(10);

    private async Task WaitUntilStoppedAsync(IReadOnlyCollection<AssetEditorViewModel> closedEditors)
    {
        // A faulted or cancelled stop counts as stopped: the editor runs nothing more either way
        var stopped = Task.WhenAll(closedEditors.Select(x => x.Stopped.ContinueWith(_ => { }, TaskScheduler.Default)));
        if (stopped.IsCompleted)
            return;

        try
        {
            await stopped.WaitAsync(StopTimeout);
        }
        catch (TimeoutException)
        {
            var running = closedEditors.Where(x => !x.Stopped.IsCompleted).Select(x => x.Asset.Url);
            GlobalLogger.GetLogger(nameof(EditorCollectionViewModel)).Warning($"The editors of {string.Join(", ", running)} did not stop within {StopTimeout.TotalSeconds} seconds; the session closes anyway.");
        }
    }

    /// <summary>
    /// Saves the changes that the open editors keep outside their asset, see <see cref="AssetEditorViewModel.SaveChangesAsync"/>.
    /// </summary>
    /// <param name="logger">The log of the session save, which gets the errors.</param>
    public async Task SaveEditorChangesAsync(ILogger logger)
    {
        foreach (var editor in editors.ToList())
        {
            await editor.SaveChangesAsync(logger);
        }
    }

    public bool TryGetAssetEditor<TEditor>(AssetViewModel asset, [MaybeNullWhen(false)] out TEditor assetEditor) where TEditor : IAssetEditorViewModel
    {
        if (openedAssets.TryGetValue(asset, out var found) && found is TEditor editor)
        {
            assetEditor = editor;
            return true;
        }

        assetEditor = default;
        return false;
    }
}
