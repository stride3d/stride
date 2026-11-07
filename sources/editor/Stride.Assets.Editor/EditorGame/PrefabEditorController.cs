// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.PrefabEditor.Game;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Quantum;
using Stride.Editor.Engine;
using Stride.Engine;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The controller of the game of a prefab editor: the entities of the prefab are cloned to the game side and loaded in
/// the game, as in the WPF <c>PrefabEditorController</c>.
/// </summary>
public sealed class PrefabEditorController : EntityHierarchyEditorController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabEditorController"/> class (UI thread).
    /// </summary>
    /// <param name="editor">The editor of the prefab.</param>
    /// <param name="viewportService">The service that creates the viewport the game renders into.</param>
    public PrefabEditorController(PrefabEditorViewModel editor, IEditorGameViewportService viewportService)
        : base(editor, viewportService, CreateEditorGame)
    {
    }

    private new PrefabEditorGame Game => (PrefabEditorGame)base.Game;

    /// <inheritdoc />
    public override Task AddPart(EntityHierarchyItemViewModel parent, Entity assetSideEntity)
    {
        EnsureAssetAccess();

        var gameSideEntity = ClonePartForGameSide((AssetCompositeHierarchy<EntityDesign, Entity>)parent.Asset.Asset, assetSideEntity);
        return InvokeAsync(() =>
        {
            Logger.Debug($"Adding entity {assetSideEntity.Id} to game-side scene");
            if (parent is PrefabRootViewModel)
            {
                Game.LoadEntity(gameSideEntity);
            }
            else
            {
                var parentEntity = (Entity?)FindPart(parent.Id)
                                   ?? throw new InvalidOperationException($"The parent {parent.Id} of the entity {assetSideEntity.Id} is not in the game.");
                GameSideNodeContainer.GetNode(parentEntity.Transform.Children).Add(gameSideEntity.Transform);
            }
        });
    }

    /// <inheritdoc />
    public override Task RemovePart(EntityHierarchyItemViewModel parent, Entity assetSideEntity)
    {
        EnsureAssetAccess();

        var partId = new AbsoluteId(AssetId.Empty, assetSideEntity.Id);
        return InvokeAsync(() =>
        {
            Logger.Debug($"Removing entity {assetSideEntity.Id} from game-side scene");
            var entity = (Entity?)FindPart(partId)
                         ?? throw new InvalidOperationException($"The entity {assetSideEntity.Id} is not in the game.");
            if (parent is PrefabRootViewModel)
            {
                Game.UnloadEntity(entity);
            }
            else
            {
                var parentEntity = (Entity?)FindPart(parent.Id)
                                   ?? throw new InvalidOperationException($"The parent {parent.Id} of the entity {assetSideEntity.Id} is not in the game.");
                var index = parentEntity.Transform.Children.IndexOf(entity.Transform);
                GameSideNodeContainer.GetNode(parentEntity.Transform.Children).Remove(entity.Transform, new NodeIndex(index));
            }
        });
    }

    /// <summary>
    /// Loads the entities of <paramref name="root"/> into the game (UI thread).
    /// </summary>
    /// <param name="root">The root of the prefab.</param>
    public Task LoadEntities(PrefabRootViewModel root)
    {
        EnsureAssetAccess();

        var gameSideEntities = ClonePartsForGameSide((AssetCompositeHierarchy<EntityDesign, Entity>)root.Asset.Asset, root.InnerSubEntities.Select(x => x.AssetSideEntity)).ToList();
        return InvokeAsync(() => Game.LoadEntities(gameSideEntities));
    }

    /// <inheritdoc />
    public override AbsoluteId GetAbsoluteId(Entity entity)
    {
        return new AbsoluteId(Asset.Id, entity.Id);
    }

    /// <inheritdoc/>
    protected override object? FindPart(AbsoluteId id)
    {
        return Game.FindSubEntity(Guid.Empty, id.ObjectId);
    }

    private static EntityHierarchyEditorGame CreateEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string? effectLogPath)
    {
        return new PrefabEditorGame(gameContentLoadedTaskSource, effectCompiler, effectLogPath);
    }
}
