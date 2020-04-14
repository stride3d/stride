// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.PrefabEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.PrefabEditor.ViewModels;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;
using Xenko.Shaders.Compiler;

namespace Xenko.Assets.Presentation.AssetEditors.PrefabEditor.Services
{
    public sealed class PrefabEditorController : EntityHierarchyEditorController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabEditorController"/> class.
        /// </summary>
        /// <param name="asset">The prefab associated with this instance.</param>
        /// <param name="editor">The editor associated with this instance.</param>
        public PrefabEditorController([NotNull] AssetViewModel asset, [NotNull] PrefabEditorViewModel editor)
            : base(asset, editor, CreateEditorGame)
        {
        }

        private new PrefabEditorGame Game => (PrefabEditorGame)base.Game;

        /// <inheritdoc />
        public override Task AddPart([NotNull] EntityHierarchyElementViewModel parent, Entity assetSidePart)
        {
            EnsureAssetAccess();

            var gameSidePart = ClonePartForGameSide(parent.Asset.Asset, assetSidePart);
            return InvokeAsync(() =>
            {
                Logger.Debug($"Adding entity {assetSidePart.Id} to game-side scene");
                if (parent is PrefabRootViewModel)
                {
                    Game.LoadEntity(gameSidePart);
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
        public override Task RemovePart([NotNull] EntityHierarchyElementViewModel parent, Entity assetSidePart)
        {
            EnsureAssetAccess();

            return InvokeAsync(() =>
            {
                Logger.Debug($"Removing entity {assetSidePart.Id} from game-side scene");
                var partId = new AbsoluteId(AssetId.Empty, assetSidePart.Id);
                var part = (Entity)FindPart(partId);
                if (part == null)
                    throw new InvalidOperationException($"The given {nameof(assetSidePart.Id)} does not correspond to any existing part.");

                if (parent is PrefabRootViewModel)
                {
                    Game.UnloadEntity(part);
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
        /// Loads the entities of specified <paramref name="root"/> into the game.
        /// </summary>
        /// <param name="root">A root containing entities to load into the game.</param>
        [NotNull]
        public Task LoadEntities([NotNull] PrefabRootViewModel root)
        {
            EnsureAssetAccess();

            var gameSideEntities = ClonePartsForGameSide(root.Asset.Asset, root.InnerSubEntities.Select(x => x.AssetSideEntity));
            return InvokeAsync(() => Game.LoadEntities(gameSideEntities));
        }

        /// <summary>
        /// Removes the entities of specified <paramref name="root"/> from the game.
        /// </summary>
        /// <param name="root">A root containing entities to remove from the game.</param>
        [NotNull]
        public Task UnloadEntities([NotNull] PrefabRootViewModel root)
        {
            EnsureAssetAccess();

            var entityIds = root.InnerSubEntities.Select(e => e.Id).ToList();
            return InvokeAsync(() =>
            {
                var gameSideEntities = entityIds.Select(FindPart).Cast<Entity>().NotNull();
                Game.UnloadEntities(gameSideEntities);
            });
        }

        /// <inheritdoc />
        public override AbsoluteId GetAbsoluteId(Entity entity)
        {
            return new AbsoluteId(Asset.Id, entity.Id);
        }

        /// <inheritdoc/>
        protected override object FindPart(AbsoluteId id)
        {
            return Game.FindSubEntity(Guid.Empty, id.ObjectId);
        }

        /// <inheritdoc/>
        protected override void InitializeServices(EditorGameServiceRegistry serviceRegistry)
        {
            base.InitializeServices(serviceRegistry);
            serviceRegistry.Add(new PrefabEditorLightService());
        }

        [NotNull]
        private static PrefabEditorGame CreateEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string effectLogPath)
        {
            return new PrefabEditorGame(gameContentLoadedTaskSource, effectCompiler, effectLogPath);
        }
    }
}
