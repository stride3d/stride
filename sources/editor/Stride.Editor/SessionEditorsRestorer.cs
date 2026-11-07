// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Serialization;

namespace Stride.Editor;

/// <summary>
/// What opens with a session, as in the WPF Game Studio: the editors that were open when the session last closed, or its default
/// scene when it has none (first open, new project). While the session is open, the list of open editors follows in the
/// <see cref="MRUAdditionalData"/> of the session, which both editors read and write.
/// </summary>
public sealed class SessionEditorsRestorer
{
    private readonly SessionViewModel session;
    private readonly MRUAdditionalDataCollection data;
    private readonly UFile sessionPath;
    private readonly List<AssetId> keptAssets = [];
    private bool tracking;

    /// <param name="session">The open session.</param>
    /// <param name="data">What the editors keep for each recent session.</param>
    /// <param name="sessionPath">The key of the session in <paramref name="data"/>: its solution, as WPF's.</param>
    public SessionEditorsRestorer(SessionViewModel session, MRUAdditionalDataCollection data, UFile sessionPath)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(sessionPath);
        this.session = session;
        this.data = data;
        this.sessionPath = sessionPath;
    }

    /// <summary>
    /// Opens the editors of the last session and selects the asset of the last one; or, when the session kept none, opens the
    /// default scene of the current project and selects it. Then the list of open editors is saved at each change, until
    /// <see cref="Stop"/>.
    /// </summary>
    /// <remarks>
    /// An asset whose editor exists only in the other editor (WPF) stays in the list, so that it opens there next time.
    /// </remarks>
    public void Restore()
    {
        var assets = (data.GetData(sessionPath)?.OpenedAssets ?? []).Select(session.GetAssetById).OfType<AssetViewModel>().Where(IsSaved).ToList();
        if (assets.Count > 0)
        {
            session.AssetCollection.SelectAssets([assets[^1]]);
            foreach (var asset in assets)
            {
                if (HasEditor(asset))
                    session.EditorCollection.OpenAssetEditor(asset);
                else
                    keptAssets.Add(asset.Id);
            }
        }
        // WPF opens the default scene only when the session kept no asset; it also does when none of them exists anymore
        else if (FindDefaultScene(session) is { } defaultScene)
        {
            session.AssetCollection.SelectAssets([defaultScene]);
            session.EditorCollection.OpenAssetEditor(defaultScene);
        }

        tracking = true;
        ((INotifyCollectionChanged)session.EditorCollection.Editors).CollectionChanged += EditorsChanged;
        Save();
    }

    /// <summary>
    /// Saves the list of open editors one last time and stops following it, before the session closes its editors.
    /// </summary>
    public void Stop()
    {
        if (!tracking)
            return;

        Save();
        tracking = false;
        ((INotifyCollectionChanged)session.EditorCollection.Editors).CollectionChanged -= EditorsChanged;
    }

    /// <summary>
    /// Finds the default scene of the game settings of the current project, or of its dependencies, as the WPF Game Studio does.
    /// </summary>
    /// <returns>The scene, or <c>null</c> when there is no current project, no game settings or no default scene.</returns>
    public static AssetViewModel? FindDefaultScene(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var project = session.CurrentProject;
        if (project is null)
            return null;

        var gameSettings = FindGameSettings(project);
        if (gameSettings is null)
        {
            // Scan the dependencies for the game settings
            foreach (var dependency in project.PackageContainer.FlattenedDependencies)
            {
                if (dependency.Package is null)
                    continue;

                var dependencyViewModel = session.AllPackages.FirstOrDefault(x => x.Package == dependency.Package);
                gameSettings = dependencyViewModel is not null ? FindGameSettings(dependencyViewModel) : null;
                if (gameSettings is not null)
                    break;
            }
        }

        var defaultScene = (gameSettings?.Asset as GameSettingsAsset)?.DefaultScene;
        if (defaultScene is null)
            return null;

        var reference = AttachedReferenceManager.GetAttachedReference(defaultScene);
        return reference is not null ? session.GetAssetById(reference.Id) : null;
    }

    private static AssetViewModel? FindGameSettings(PackageViewModel package)
    {
        return package.Assets.FirstOrDefault(x => x.AssetItem.UnqualifiedUrl == GameSettingsAsset.GameSettingsLocation);
    }

    private bool HasEditor(AssetViewModel asset)
    {
        return session.ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewModelType(asset.GetType()) is not null;
    }

    // As WPF, the deleted assets and those that cannot be edited are not saved
    private static bool IsSaved(AssetViewModel asset) => !asset.IsDeleted && asset.IsEditable;

    private void EditorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Save();
    }

    private void Save()
    {
        var kept = keptAssets.Select(session.GetAssetById).OfType<AssetViewModel>();
        var open = session.EditorCollection.Editors.Select(x => x.Asset);
        data.UpdateOpenedAssets(sessionPath, kept.Concat(open).Where(IsSaved).Select(x => x.Id).Distinct().ToList());
    }
}
