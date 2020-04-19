// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Editor.Engine;
using Stride.Engine;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Presentation.AssetEditors.SceneEditor.Game
{
    public sealed class SceneEditorGame : EntityHierarchyEditorGame
    {
        private readonly Dictionary<Guid, Scene> scenes = new Dictionary<Guid, Scene>();

        public SceneEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string effectLogPath)
            : base(gameContentLoadedTaskSource, effectCompiler, effectLogPath)
        {

        }

        public event Action<Scene> SceneAdded;
        public event Action<Scene> SceneRemoved;

        /// <summary>
        /// Adds a new scene with the provided <paramref name="sceneId"/>.
        /// </summary>
        /// <param name="sceneId">The identifier of the scene to add.</param>
        /// <param name="parentId">The identifier of an existing parent scene, or <see cref="Guid.Empty"/>.</param>
        public void AddScene(Guid sceneId, Guid parentId)
        {
            if (sceneId == Guid.Empty) throw new InvalidOperationException($"{nameof(sceneId)} cannot be {nameof(Guid.Empty)}.");
            if (scenes.ContainsKey(sceneId)) throw new InvalidOperationException($"A scene matching the given {sceneId}, already exists.");

            EnsureContentScene();

            var scene = new Scene { Id = sceneId };
            var parent = parentId != Guid.Empty ? GetScene(parentId) : ContentScene;
            AddSceneToParent(scene, parent);
            scenes.Add(sceneId, scene);
            SceneAdded?.Invoke(scene);
        }

        /// <summary>
        /// Moves an existing scene identified by <paramref name="sceneId"/> under the parent scene identified by <paramref name="parentId"/>.
        /// </summary>
        /// <param name="sceneId">The identifier of the scene to move.</param>
        /// <param name="parentId">The identifier of an existing parent scene, or <see cref="Guid.Empty"/>.</param>
        public void MoveScene(Guid sceneId, Guid parentId)
        {
            if (sceneId == Guid.Empty)
                throw new InvalidOperationException($"{nameof(sceneId)} cannot be {nameof(Guid.Empty)}.");

            EnsureContentScene();

            var scene = GetScene(sceneId);
            var oldParentId = scene.Parent?.Id ?? Guid.Empty;
            if (oldParentId == parentId)
                return;

            RemoveSceneFromParent(scene);
            var newParent = parentId != Guid.Empty ? GetScene(parentId) : ContentScene;
            AddSceneToParent(scene, newParent);
        }

        /// <summary>
        /// Removes the existing scene identified by <paramref name="sceneId"/>.
        /// </summary>
        /// <param name="sceneId">The identifier of the scene to remove.</param>
        /// <remarks>
        /// The scene must be empty, i.e. its child scenes must have been removed first and its entities unloaded.
        /// </remarks>
        public void RemoveScene(Guid sceneId)
        {
            if (sceneId == Guid.Empty)
                throw new InvalidOperationException($"{nameof(sceneId)} cannot be {nameof(Guid.Empty)}.");

            EnsureContentScene();

            var scene = GetScene(sceneId);
            if (scene.Children.Count > 0)
                throw new InvalidOperationException("Scenes with child scenes cannot be removed.");
            if (scene.Entities.Count > 0)
                throw new InvalidOperationException("Scenes with entities cannot be removed.");

            SceneRemoved?.Invoke(scene);
            RemoveSceneFromParent(scene);
            scenes.Remove(sceneId);
        }

        /// <inheritdoc/>
        public override Entity FindSubEntity(Guid sceneId, Guid entityId)
        {
            EnsureContentScene();

            Scene scene;
            if (scenes.TryGetValue(sceneId, out scene))
            {
                Entity entity;
                // Note: special case of the virtual anchor (sceneID == entityId), own by the parent scene
                if (sceneId == entityId)
                    entity = scene.Parent?.FindSubEntity(entityId);
                else
                    entity = scene.FindSubEntity(entityId);

                if (entity != null)
                    return entity;
            }

            // Fall back to the content scene itself
            return ContentScene.FindSubEntity(entityId);
        }

        /// <summary>
        /// Loads the specified root <paramref name="entities"/> into the content scene.
        /// </summary>
        /// <param name="entities">A collection of entities to load into the content scene.</param>
        /// <param name="sceneId">The identifier of the scene the given <paramref name="entities"/> belong to.</param>
        /// <remarks>
        /// If <paramref name="sceneId"/> is <see cref="Guid.Empty"/>, the provided <paramref name="entities"/> will be loaded
        /// into the <see cref="EntityHierarchyEditorGame.ContentScene"/>; otherwise, they will be loaded into a separate child scene.
        /// </remarks>
        public void LoadEntities([ItemNotNull, NotNull]  IEnumerable<Entity> entities, Guid sceneId)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (sceneId == Guid.Empty) throw new InvalidOperationException($"{nameof(sceneId)} cannot be {nameof(Guid.Empty)}.");

            EnsureContentScene();

            var scene = GetScene(sceneId);
            scene.Entities.AddRange(entities);
        }

        /// <summary>
        /// Loads the specified root <paramref name="entity"/> into the content scene.
        /// </summary>
        /// <param name="entity">An entity to load into the content scene.</param>
        /// <param name="sceneId">The identifier of the scene the given <paramref name="entity"/> belongs to.</param>
        /// <remarks>
        /// If <paramref name="sceneId"/> is <see cref="Guid.Empty"/>, the provided <paramref name="entity"/> will be loaded
        /// into the <see cref="EntityHierarchyEditorGame.ContentScene"/>; otherwise, it will be loaded into a separate child scene.
        /// </remarks>
        public void LoadEntity([NotNull] Entity entity, Guid sceneId)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (sceneId == Guid.Empty) throw new InvalidOperationException($"{nameof(sceneId)} cannot be {nameof(Guid.Empty)}.");

            EnsureContentScene();

            var scene = GetScene(sceneId);
            scene.Entities.Add(entity);
        }

        /// <summary>
        /// Removes the specified root <paramref name="entities"/> from the content scene.
        /// </summary>
        /// <param name="entities">A collection of entities to remove from the content scene.</param>
        /// <param name="sceneId">The identifier of the scene the given <paramref name="entities"/> belong to.</param>
        public void UnloadEntities([ItemNotNull, NotNull]  IEnumerable<Entity> entities, Guid sceneId)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (sceneId == Guid.Empty) throw new InvalidOperationException($"{nameof(sceneId)} cannot be {nameof(Guid.Empty)}.");

            EnsureContentScene();

            var scene = GetScene(sceneId);
            foreach (var entity in entities)
            {
                scene.Entities.Remove(entity);
            }
        }

        /// <summary>
        /// Removes the specified root <paramref name="entity"/> from the content scene.
        /// </summary>
        /// <param name="entity">An entity to remove from the content scene.</param>
        /// <param name="sceneId">The identifier of the scene the given <paramref name="entity"/> belongs to.</param>
        public void UnloadEntity([NotNull] Entity entity, Guid sceneId)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (sceneId == Guid.Empty) throw new InvalidOperationException($"{nameof(sceneId)} cannot be {nameof(Guid.Empty)}.");

            EnsureContentScene();

            var scene = GetScene(sceneId);
            scene.Entities.Remove(entity);
        }

        public void UpdateSceneAnchor(Guid sceneId, ref Vector3 position, ref Quaternion rotation, ref Vector3 scale)
        {
            // Get virtual anchor
            var anchor = GetScene(sceneId).Parent?.FindSubEntity(sceneId);
            if (anchor == null)
                return;
            anchor.Transform.Position = position;
            // Note: rotation and scale are currently ignored
        }

        private void AddSceneToParent([NotNull] Scene scene, [NotNull] Scene parent)
        {
            // Add to parent
            parent.Children.Add(scene);
            // Add virtual anchor
            var anchorEntity = new Entity
            {
                Id = scene.Id,
                Name = $"Virtual anchor of scene {scene.Id}"
            };
            // Disable component gizmo
            anchorEntity.Tags.Add(GizmoBase.NoGizmoKey, true);
            parent.Entities.Add(anchorEntity);
            // Create script for virtual anchor
            var anchorScript = new VirtualAnchorScript();
            anchorEntity.Components.Add(anchorScript);
            // Note: because the script processor is disabled in editor game, we have to add the script manually
            Script.Add(anchorScript);
        }

        private void RemoveSceneFromParent([NotNull] Scene scene)
        {
            // Remove from parent
            var parent = scene.Parent;
            parent.Children.Remove(scene);

            // Find virtual anchor
            var anchorIndex = parent.Entities.IndexOf(x => x.Id == scene.Id);
            if (anchorIndex != -1)
            {
                var anchorEntity = parent.Entities[anchorIndex].Components.OfType<VirtualAnchorScript>().FirstOrDefault();
                // Remove anchor script
                if (anchorEntity != null)
                    Script.Remove(anchorEntity);
                // Remove virtual anchor
                parent.Entities.RemoveAt(anchorIndex);
            }
        }

        [NotNull]
        private Scene GetScene(Guid sceneId)
        {
            if (!scenes.TryGetValue(sceneId, out Scene scene))
                throw new InvalidOperationException($"No scene match the given {sceneId}");

            return scene;
        }

        /// <summary>
        /// Script that synchronizes the scene offset to the virtual anchor position.
        /// </summary>
        /// <remarks>
        /// We use an indirection with a virtual anchor to be able to display a gizmo that the user can manipulate.
        /// In a real game, this indirection is not needed, and the scene offset should be directly modified instead.
        /// </remarks>
        private class VirtualAnchorScript : AsyncScript
        {
            private new SceneEditorGame Game => (SceneEditorGame)base.Game;

            /// <inheritdoc />
            public override async Task Execute()
            {
                var oldPositon = Vector3.Zero;
                // Note: rotation and scale are currently ignored
                while (Game.IsRunning)
                {
                    var newPosition = Entity.Transform.Position;
                    if (newPosition != oldPositon)
                    {
                        // Note: anchor id is equal to the corresponding scene id
                        var scene = Game.GetScene(Entity.Id);
                        scene.Offset = newPosition;
                        oldPositon = newPosition;
                    }
                    await Script.NextFrame();
                }
            }
        }
    }
}
