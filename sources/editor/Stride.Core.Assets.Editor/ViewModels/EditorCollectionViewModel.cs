// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

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

    /// <summary>
    /// Closes the editor of an asset once it agrees (see <see cref="AssetEditorViewModel.PreviewCloseAsync"/>): it saves its
    /// changes or drops them as <paramref name="save"/> says, or as the user chooses when it is <c>null</c>.
    /// </summary>
    /// <param name="asset">The asset whose editor closes.</param>
    /// <param name="save"><c>true</c> to save the changes, <c>false</c> to drop them, <c>null</c> to ask the user.</param>
    /// <returns><c>true</c> when the editor is closed or the asset has none, <c>false</c> when the user kept it open.</returns>
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

    /// <summary>
    /// Closes every editor, see <see cref="CloseAssetEditorAsync"/>. It stops at the first editor that the user keeps open;
    /// the editors closed before it stay closed.
    /// </summary>
    /// <param name="save"><c>true</c> to save the changes, <c>false</c> to drop them, <c>null</c> to ask the user.</param>
    /// <returns><c>true</c> when every editor is closed, <c>false</c> when the user kept one open.</returns>
    public async Task<bool> CloseAllEditorsAsync(bool? save)
    {
        foreach (var editor in editors.ToList())
        {
            if (!await CloseAssetEditorAsync(editor.Asset, save))
                return false;
        }
        return true;
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

    public bool TryGetAssetEditor<TEditor>(AssetViewModel asset, [MaybeNullWhen(false)] out TEditor assetEditor) where TEditor : AssetEditorViewModel
    {
        if (openedAssets.TryGetValue(asset, out var found) && found is TEditor editor)
        {
            assetEditor = editor;
            return true;
        }

        assetEditor = null;
        return false;
    }
}
