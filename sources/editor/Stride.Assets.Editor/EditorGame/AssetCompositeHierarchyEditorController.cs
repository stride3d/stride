// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The controller of the game of an editor of a composite hierarchy asset: the parts of the asset are cloned to the
/// game side.
/// </summary>
/// <typeparam name="TEditorGame">The type of the game.</typeparam>
/// <typeparam name="TAssetPartDesign">The type of the part designs of the asset.</typeparam>
/// <typeparam name="TAssetPart">The type of the parts of the asset.</typeparam>
public abstract class AssetCompositeHierarchyEditorController<TEditorGame, TAssetPartDesign, TAssetPart> : EditorGameController<TEditorGame>
    where TEditorGame : EditorServiceGame
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>, new()
    where TAssetPart : class, IIdentifiable
{
    /// <inheritdoc />
    protected AssetCompositeHierarchyEditorController(AssetEditorViewModel editor, IEditorGameViewportService viewportService, EditorGameFactory<TEditorGame> gameFactory)
        : base(editor, viewportService, gameFactory)
    {
    }

    /// <summary>
    /// Clones a part of the asset, with its sub-hierarchy, to the game side (UI thread).
    /// </summary>
    protected TAssetPart ClonePartForGameSide(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> asset, TAssetPart part)
    {
        return ClonePartsForGameSide(asset, [part]).Single();
    }

    /// <summary>
    /// Clones parts of the asset, with their sub-hierarchies, to the game side (UI thread). The references to other parts
    /// are resolved to the game-side objects when they exist, cleared otherwise.
    /// </summary>
    protected IEnumerable<TAssetPart> ClonePartsForGameSide(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> asset, IEnumerable<TAssetPart> parts)
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
                    accessor.UpdateValue(null!);
                }
            }
        }
        return clone.RootParts;
    }

    /// <summary>
    /// Collects the identifiable objects already on the game side, by identifier.
    /// </summary>
    protected abstract Dictionary<Guid, IIdentifiable> CollectIdentifiableObjects();
}
