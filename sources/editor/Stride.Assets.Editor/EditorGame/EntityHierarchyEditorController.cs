// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
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
