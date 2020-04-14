// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.Services
{
    public abstract class AssetCompositeHierarchyEditorController<TEditorGame, TAssetPartDesign, TAssetPart, TParentViewModel> : EditorGameController<TEditorGame>
        where TEditorGame : EditorServiceGame
        where TParentViewModel : AssetCompositeItemViewModel
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>, new()
        where TAssetPart : class, IIdentifiable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCompositeHierarchyEditorController{TEditorGame,TAssetPartDesign,TAssetPart,TParentViewModel}"/> class.
        /// </summary>
        /// <param name="asset">The asset associated with this instance.</param>
        /// <param name="editor">The editor associated with this instance.</param>
        /// <param name="gameFactory">The factory to create the editor game.</param>
        protected AssetCompositeHierarchyEditorController([NotNull] AssetViewModel asset, [NotNull] GameEditorViewModel editor, [NotNull] EditorGameFactory<TEditorGame> gameFactory)
            : base(asset, editor, gameFactory)
        {
        }

        /// <summary>
        /// Adds the provided <paramref name="assetSidePart"/> to the scene game.
        /// </summary>
        /// <param name="parent">The parent of the item.</param>
        /// <param name="assetSidePart"></param>
        /// <returns>An awaitable task to be notified when this operation has completed.</returns>
        [NotNull]
        public abstract Task AddPart(TParentViewModel parent, [NotNull] TAssetPart assetSidePart);

        /// <summary>
        /// Removes the provided <paramref name="assetSidePart"/> from the scene game.
        /// </summary>
        /// <param name="parent">The parent of the item before removal.</param>
        /// <param name="assetSidePart"></param>
        /// <returns>An awaitable task to be notified when this operation has completed.</returns>
        [NotNull]
        public abstract Task RemovePart(TParentViewModel parent, [NotNull] TAssetPart assetSidePart);

        /// <summary>
        /// Clones the given part in order to be used on the game-side.
        /// </summary>
        /// <param name="asset">The asset the part belongs to.</param>
        /// <param name="part">The part to clone.</param>
        /// <returns></returns>
        [NotNull]
        protected TAssetPart ClonePartForGameSide([NotNull] AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> asset, [NotNull] TAssetPart part)
        {
            return ClonePartsForGameSide(asset, part.Yield()).Single();
        }

        /// <summary>
        /// Clones the given parts in order to be used on the game-side.
        /// </summary>
        /// <param name="asset">The asset the parts belong to.</param>
        /// <param name="parts">The parts to clone.</param>
        /// <returns></returns>
        /// <remarks>The parts passed to this methods must be independent in the hierarchy.</remarks>
        [NotNull]
        protected IEnumerable<TAssetPart> ClonePartsForGameSide([NotNull] AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> asset, [NotNull] IEnumerable<TAssetPart> parts)
        {
            var flags = SubHierarchyCloneFlags.RemoveOverrides;
            var sourceContainer = Asset.PropertyGraph.Container.NodeContainer;
            var targetContainer = GameSideNodeContainer;
            var clone = AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>.CloneSubHierarchies(sourceContainer, targetContainer, asset, parts.Select(p => p.Id), flags, out _);

            // Collect external references after cloning, we need to fix them up!
            var rootNode = GameSideNodeContainer.GetOrCreateNode(clone);
            var definition = AssetQuantumRegistry.GetDefinition(asset.GetType());
            var unresolvedReferences = ExternalReferenceCollector.GetExternalReferenceAccessors(definition, rootNode);

            // Retrieve all available game-side identifiable objects, so we can try to resolve external references with items from this collection.
            var identifiableObjects = CollectIdentifiableObjects();
            foreach (var reference in unresolvedReferences)
            {
                if (identifiableObjects.TryGetValue(reference.Key.Id, out var realObject))
                {
                    // Target object found, let's update the reference with the real game-side object.
                    foreach (var accessor in reference.Value)
                    {
                        accessor.UpdateValue(realObject);
                    }
                }
                else
                {
                    // Target object not found, let's clear the reference since the currently set object could be asset-side, or a temporary proxy, etc.
                    foreach (var accessor in reference.Value)
                    {
                        accessor.UpdateValue(null);
                    }
                }
            }
            return clone.RootParts;
        }

        /// <summary>
        /// Collects all game-side identifiable objects for this asset.
        /// </summary>
        /// <returns>A dictionary containing all identifiable objects of this asset, indexed by Guid.</returns>
        [NotNull]
        protected abstract Dictionary<Guid, IIdentifiable> CollectIdentifiableObjects();
    }
}
