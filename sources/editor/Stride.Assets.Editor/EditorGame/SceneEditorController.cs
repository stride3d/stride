// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Game;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Editor.Engine;
using Stride.Engine;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The controller of the game of a scene editor: the scenes are cloned to the game side and loaded in the game.
/// </summary>
public sealed class SceneEditorController : EntityHierarchyEditorController, ISceneEditorController
{
    private readonly object lookupMutex = new();
    private readonly Dictionary<AssetId, Guid> assetToGameLookup = [];
    private readonly Dictionary<Guid, AssetId> gameToAssetLookup = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneEditorController"/> class (UI thread).
    /// </summary>
    /// <param name="editor">The editor of the scene.</param>
    /// <param name="viewportService">The service that creates the viewport the game renders into.</param>
    public SceneEditorController(SceneEditorViewModel editor, IEditorGameViewportService viewportService)
        : base(editor, viewportService, CreateEditorGame)
    {
    }

    private new SceneEditorGame Game => (SceneEditorGame)base.Game;

    /// <summary>
    /// Loads the scenes represented by the provided <paramref name="roots"/> into the game (UI thread).
    /// </summary>
    /// <param name="roots">The roots representing the scenes to be loaded.</param>
    /// <seealso cref="SceneEditorGame.AddScene"/>
    /// <seealso cref="SceneEditorGame.LoadEntities"/>
    /// <seealso cref="SceneEditorGame.UpdateSceneAnchor"/>
    public async Task LoadScenes(IReadOnlyCollection<SceneRootViewModel> roots)
    {
        EnsureAssetAccess();

        lock (lookupMutex)
        {
            // Update the lookup tables
            foreach (var root in roots)
            {
                var assetId = root.Id.AssetId;
                var sceneId = root.Id.ObjectId;
                assetToGameLookup[assetId] = sceneId;
                gameToAssetLookup[sceneId] = assetId;
            }
        }
        // The child scenes are not supported yet: each scene is a root, at the origin
        var scenesToLoad = roots.Select(root => new
        {
            Id = root.Id.ObjectId,
            ParentId = Guid.Empty,
            GameSideEntities = ClonePartsForGameSide((AssetCompositeHierarchy<EntityDesign, Entity>)root.Asset.Asset, root.InnerSubEntities.Select(x => x.AssetSideEntity)).ToList(),
        }).ToList();
        await InvokeAsync(() =>
        {
            foreach (var scene in scenesToLoad)
            {
                var position = Vector3.Zero;
                var rotation = Quaternion.Identity;
                var scale = Vector3.One;
                Logger.Debug($"Loading scene {scene.Id}");
                Game.AddScene(scene.Id, scene.ParentId);
                Logger.Debug($"Loading entities of scene {scene.Id}");
                Game.LoadEntities(scene.GameSideEntities, scene.Id);
                Logger.Debug($"Updating anchor of scene {scene.Id}");
                Game.UpdateSceneAnchor(scene.Id, ref position, ref rotation, ref scale);
            }
        });
    }

    /// <summary>
    /// Unloads the scenes represented by the provided <paramref name="roots"/> from the game (UI thread).
    /// </summary>
    /// <param name="roots">The roots representing the scenes to be unloaded.</param>
    /// <seealso cref="SceneEditorGame.UnloadEntities"/>
    /// <seealso cref="SceneEditorGame.RemoveScene"/>
    public async Task UnloadScenes(IEnumerable<SceneRootViewModel> roots)
    {
        EnsureAssetAccess();

        var scenesToUnload = roots.Select(root => new
        {
            Id = root.Id.ObjectId,
            root.Id.AssetId,
            EntityIds = root.InnerSubEntities.Select(e => e.Id).ToList(),
        }).ToList();
        await InvokeAsync(() =>
        {
            foreach (var scene in scenesToUnload)
            {
                var gameSideEntities = scene.EntityIds.Select(FindPart).OfType<Entity>();
                Logger.Debug($"Unloading entities of scene {scene.Id}");
                Game.UnloadEntities(gameSideEntities, scene.Id);
                Logger.Debug($"Removing scene {scene.Id}");
                Game.RemoveScene(scene.Id);
            }
        });
        lock (lookupMutex)
        {
            // Update the lookup tables
            foreach (var scene in scenesToUnload)
            {
                assetToGameLookup.Remove(scene.AssetId);
                gameToAssetLookup.Remove(scene.Id);
            }
        }
    }

    /// <inheritdoc />
    public override AbsoluteId GetAbsoluteId(Entity entity)
    {
        lock (lookupMutex)
        {
            var sceneId = entity.Scene.Id;
            var assetId = gameToAssetLookup[sceneId];
            return new AbsoluteId(assetId, entity.Id);
        }
    }

    /// <inheritdoc />
    public AssetId GetSceneAssetId(Scene scene)
    {
        lock (lookupMutex)
        {
            return gameToAssetLookup.TryGetValue(scene.Id, out var assetId) ? assetId : AssetId.Empty;
        }
    }

    /// <inheritdoc/>
    protected override object? FindPart(AbsoluteId id)
    {
        lock (lookupMutex)
        {
            return assetToGameLookup.TryGetValue(id.AssetId, out var sceneId) ? Game.FindSubEntity(sceneId, id.ObjectId) : null;
        }
    }

    private static EntityHierarchyEditorGame CreateEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string? effectLogPath)
    {
        return new SceneEditorGame(gameContentLoadedTaskSource, effectCompiler, effectLogPath);
    }
}
