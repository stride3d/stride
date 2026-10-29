// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Extensions;
using Stride.Editor.Engine;
using Stride.Engine;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The controller of the game of an entity hierarchy editor (scene or prefab).
/// </summary>
public abstract class EntityHierarchyEditorController : AssetCompositeHierarchyEditorController<EntityHierarchyEditorGame, EntityDesign, Entity>, IEntityHierarchyEditorController
{
    /// <inheritdoc />
    protected EntityHierarchyEditorController(AssetEditorViewModel editor, IEditorGameViewportService viewportService, EditorGameFactory<EntityHierarchyEditorGame> gameFactory)
        : base(editor, viewportService, gameFactory)
    {
    }

    /// <inheritdoc />
    public override async Task<bool> CreateScene()
    {
        await InvokeAsync(Game.InitializeContentScene);
        return true;
    }

    /// <inheritdoc />
    public abstract AbsoluteId GetAbsoluteId(Entity entity);

    /// <summary>
    /// Clones an entity the asset just got, with its children, and adds the clone to the game (UI thread).
    /// </summary>
    /// <param name="parent">The item the entity was added to: the root of the hierarchy or another entity.</param>
    /// <param name="assetSideEntity">The asset-side entity.</param>
    /// <returns>A task that completes once the clone is in the game.</returns>
    public abstract Task AddPart(EntityHierarchyItemViewModel parent, Entity assetSideEntity);

    /// <summary>
    /// Removes from the game the clone of an entity the asset just lost, with its children (UI thread).
    /// </summary>
    /// <param name="parent">The item the entity was removed from: the root of the hierarchy or another entity.</param>
    /// <param name="assetSideEntity">The asset-side entity.</param>
    /// <returns>A task that completes once the clone is out of the game.</returns>
    public abstract Task RemovePart(EntityHierarchyItemViewModel parent, Entity assetSideEntity);

    /// <inheritdoc/>
    protected override Dictionary<Guid, IIdentifiable> CollectIdentifiableObjects()
    {
        var allEntities = new[] { Game.ContentScene }.BreadthFirst(x => x.Children).SelectMany(x => x.Entities).BreadthFirst(x => x.Transform.Children.Select(y => y.Entity));
        var definition = AssetQuantumRegistry.GetDefinition(Asset.Asset.GetType());
        var identifiableObjects = new Dictionary<Guid, IIdentifiable>();
        foreach (var entityNode in allEntities.Select(x => GameSideNodeContainer.GetOrCreateNode(x)))
        {
            foreach (var identifiable in IdentifiableObjectCollector.Collect(definition, entityNode))
            {
                identifiableObjects.Add(identifiable.Key, identifiable.Value);
            }
        }
        return identifiableObjects;
    }
}
