// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Annotations;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Xenko.Editor.Engine;
using Xenko.Engine;
using Xenko.Shaders.Compiler;

namespace Xenko.Assets.Presentation.AssetEditors.PrefabEditor.Game
{
    public sealed class PrefabEditorGame : EntityHierarchyEditorGame
    {
        public PrefabEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string effectLogPath)
            : base(gameContentLoadedTaskSource, effectCompiler, effectLogPath)
        {
        }

        /// <inheritdoc/>
        public override Entity FindSubEntity(Guid sceneId, Guid entityId)
        {
            EnsureContentScene();
            return ContentScene.FindSubEntity(entityId);
        }

        /// <summary>
        /// Loads the specified root <paramref name="entities"/> into the content scene.
        /// </summary>
        /// <param name="entities">A collection of entities to load into the content scene.</param>
        public void LoadEntities([ItemNotNull, NotNull]  IEnumerable<Entity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            EnsureContentScene();

            ContentScene.Entities.AddRange(entities);
        }

        /// <summary>
        /// Loads the specified root <paramref name="entity"/> into the content scene.
        /// </summary>
        /// <param name="entity">An entity to load into the content scene.</param>
        public void LoadEntity([NotNull] Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            EnsureContentScene();

            ContentScene.Entities.Add(entity);
        }

        /// <summary>
        /// Removes the specified root <paramref name="entities"/> from the content scene.
        /// </summary>
        /// <param name="entities">A collection of entities to remove from the content scene.</param>
        public void UnloadEntities([ItemNotNull, NotNull]  IEnumerable<Entity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            EnsureContentScene();

            foreach (var entity in entities)
            {
                ContentScene.Entities.Remove(entity);
            }
        }

        /// <summary>
        /// Removes the specified root <paramref name="entity"/> from the content scene.
        /// </summary>
        /// <param name="entity">An entity to remove from the content scene.</param>
        public void UnloadEntity([NotNull] Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            EnsureContentScene();

            ContentScene.Entities.Remove(entity);
        }
    }
}
