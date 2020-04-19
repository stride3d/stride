// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Game;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Presentation.AssetEditors.SceneEditor.Services
{
    public sealed class SceneEditorController : EntityHierarchyEditorController
    {
        private readonly object lookupMutex = new object();
        private readonly Dictionary<AssetId, Guid> assetToGameLookup = new Dictionary<AssetId, Guid>();
        private readonly Dictionary<Guid, AssetId> gameToAssetLookup = new Dictionary<Guid, AssetId>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneEditorController"/> class.
        /// </summary>
        /// <param name="asset">The scene associated with this instance.</param>
        /// <param name="editor">The editor associated with this instance.</param>
        public SceneEditorController([NotNull] AssetViewModel asset, [NotNull] SceneEditorViewModel editor)
            : base(asset, editor, CreateEditorGame)
        {
        }

        private new SceneEditorGame Game => (SceneEditorGame)base.Game;

        /// <inheritdoc />
        /// <seealso cref="SceneEditorGame.LoadEntity"/>
        public override Task AddPart([NotNull] EntityHierarchyElementViewModel parent, Entity assetSidePart)
        {
            EnsureAssetAccess();

            var gameSidePart = ClonePartForGameSide(parent.Asset.Asset, assetSidePart);
            return InvokeAsync(() =>
            {
                Logger.Debug($"Adding entity {assetSidePart.Id} to game-side scene");
                if (parent is SceneRootViewModel)
                {
                    Game.LoadEntity(gameSidePart, parent.Id.ObjectId);
                }
                else
                {
                    var parentEntity = (Entity)FindPart(parent.Id);
                    if (parentEntity == null)
                        throw new InvalidOperationException($"The given {nameof(parent.Id)} does not correspond to any existing part.");

                    GameSideNodeContainer.GetNode(parentEntity.Transform.Children).Add(gameSidePart.Transform);
                }
            });
        }

        /// <inheritdoc />
        /// <seealso cref="SceneEditorGame.UnloadEntity"/>
        public override Task RemovePart([NotNull] EntityHierarchyElementViewModel parent, Entity assetSidePart)
        {
            EnsureAssetAccess();

            return InvokeAsync(() =>
            {
                Logger.Debug($"Removing entity {assetSidePart.Id} from game-side scene");
                var partId = new AbsoluteId(parent.Id.AssetId, assetSidePart.Id);
                var part = (Entity)FindPart(partId);
                if (part == null)
                    throw new InvalidOperationException($"The given {nameof(assetSidePart.Id)} does not correspond to any existing part.");

                if (parent is SceneRootViewModel)
                {
                    Game.UnloadEntity(part, parent.Id.ObjectId);
                }
                else
                {
                    var parentEntity = (Entity)FindPart(parent.Id);
                    if (parentEntity == null)
                        throw new InvalidOperationException($"The given {nameof(parent.Id)} does not correspond to any existing part.");

                    var i = parentEntity.Transform.Children.IndexOf(part.Transform);
                    GameSideNodeContainer.GetNode(parentEntity.Transform.Children).Remove(part.Transform, new NodeIndex(i));
                }
            });
        }

        /// <summary>
        /// Loads the scenes represented by the provided <paramref name="roots"/> into the game.
        /// </summary>
        /// <param name="roots">The roots representing the scenes to be loaded.</param>
        /// <seealso cref="SceneEditorGame.AddScene"/>
        /// <seealso cref="SceneEditorGame.LoadEntities"/>
        /// <seealso cref="SceneEditorGame.UpdateSceneAnchor"/>
        [NotNull]
        public async Task LoadScenes([ItemNotNull, NotNull] IReadOnlyCollection<SceneRootViewModel> roots)
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
            var scenesToLoad = roots.Select(root =>
            {
                return new
                {
                    Id = root.Id.ObjectId,
                    root.Offset,
                    parentId = root.ParentScene?.Id.ObjectId ?? Guid.Empty,
                    gameSideEntities = ClonePartsForGameSide(root.Asset.Asset, root.InnerSubEntities.Select(x => x.AssetSideEntity))
                };
            }).ToList();
            await InvokeAsync(() =>
            {
                foreach (var scene in scenesToLoad)
                {
                    var position = scene.Offset;
                    var rotation = Quaternion.Identity;
                    var scale = Vector3.One;
                    Logger.Debug($"Loading scene {scene.Id}");
                    Game.AddScene(scene.Id, scene.parentId);
                    Logger.Debug($"Loading entities of scene {scene.Id}");
                    Game.LoadEntities(scene.gameSideEntities, scene.Id);
                    Logger.Debug($"Updating anchor of scene {scene.Id}");
                    Game.UpdateSceneAnchor(scene.Id, ref position, ref rotation, ref scale);
                }
            });
        }

        /// <summary>
        /// Unloads the scenes represented by the provided <paramref name="roots"/> from the game.
        /// </summary>
        /// <param name="roots">The roots representing the scenes to be unloaded.</param>
        /// <seealso cref="SceneEditorGame.UnloadEntities"/>
        /// <seealso cref="SceneEditorGame.RemoveScene"/>
        [NotNull]
        public async Task UnloadScenes([ItemNotNull, NotNull] IEnumerable<SceneRootViewModel> roots)
        {
            EnsureAssetAccess();

            var scenesToUnload = roots.Select(root =>
            {
                return new
                {
                    Id = root.Id.ObjectId,
                    root.Id.AssetId,
                    entityIds = root.InnerSubEntities.Select(e => e.Id).ToList()
                };
            }).ToList();
            await InvokeAsync(() =>
            {
                foreach (var scene in scenesToUnload)
                {
                    var gameSideEntities = scene.entityIds.Select(FindPart).Cast<Entity>().NotNull();
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

        [NotNull]
        public Task UpdateSceneAnchorPosition([NotNull] SceneRootViewModel root)
        {
            EnsureAssetAccess();

            var sceneId = root.Id.ObjectId;
            var position = root.Offset;
            var rotation = Quaternion.Identity;
            var scale = Vector3.One;
            return InvokeAsync(() =>
            {
                Logger.Debug($"Updating anchor of scene {sceneId}");
                Game.UpdateSceneAnchor(sceneId, ref position, ref rotation, ref scale);
            });
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

        /// <summary>
        /// Looks for the asset id of a loaded scene
        /// </summary>
        /// <param name="scene">The loaded scene for which to lookup the <see cref="AssetId"/></param>
        /// <returns>The asset Id of a loaded scene or <see cref="AssetId.Empty"/> if it is not loaded</returns>
        public AssetId GetSceneAssetId([NotNull] Scene scene)
        {
            lock (lookupMutex)
            {
                return gameToAssetLookup.TryGetValue(scene.Id, out AssetId assetId) ? assetId : AssetId.Empty;
            }
        }

        /// <inheritdoc/>
        protected override object FindPart(AbsoluteId id)
        {
            lock (lookupMutex)
            {
                return assetToGameLookup.TryGetValue(id.AssetId, out Guid sceneId) ? Game.FindSubEntity(sceneId, id.ObjectId) : null;
            }
        }

        /// <inheritdoc/>
        protected override void InitializeServices(EditorGameServiceRegistry serviceRegistry)
        {
            base.InitializeServices(serviceRegistry);
            serviceRegistry.Add(new EditorGameCameraPreviewService(this));
            serviceRegistry.Add(new EditorGameNavigationMeshService(Editor));
        }

        [NotNull]
        private static SceneEditorGame CreateEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string effectLogPath)
        {
            return new SceneEditorGame(gameContentLoadedTaskSource, effectCompiler, effectLogPath);
        }
    }
}
