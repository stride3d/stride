// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The geometry of the snap to the floor: the bounds of the entities of the game, and the floor under an entity. No physics: the floor is the top of the bounding boxes of the models below, or the ground
/// plane.
/// </summary>
internal static class SceneEditingGeometry
{
    /// <summary>
    /// The height of the ground plane: the plane of the grid of the view.
    /// </summary>
    public const float GroundHeight = 0.0f;

    /// <summary>
    /// Gets the world bounding box of the models of an entity and of its descendants (game thread), or <c>null</c> when none
    /// of them has a model with bounds.
    /// </summary>
    public static BoundingBox? GetModelBounds(Entity entity)
    {
        BoundingBox? bounds = null;
        foreach (var model in SelfAndDescendants(entity).SelectMany(x => x.Components.OfType<ModelComponent>()))
        {
            if (TryGetBounds(model, out var modelBounds))
                bounds = bounds is { } merged ? BoundingBox.Merge(merged, modelBounds) : modelBounds;
        }
        return bounds;
    }

    /// <summary>
    /// Gets the world bounds of an entity and its descendants for a snap to the floor (game thread): the box of their
    /// models, or the position of the entity when they have none.
    /// </summary>
    public static BoundingBox GetSnapBounds(Entity entity)
    {
        var position = entity.Transform.WorldMatrix.TranslationVector;
        return GetModelBounds(entity) ?? new BoundingBox(position, position);
    }

    /// <summary>
    /// Gets the world bounding box of a model (game thread), when it shows and the game computed it.
    /// </summary>
    public static bool TryGetBounds(ModelComponent model, out BoundingBox bounds)
    {
        bounds = model.BoundingBox;
        return model is { Enabled: true, Model: not null } && bounds.Minimum.X <= bounds.Maximum.X;
    }

    /// <summary>
    /// Gets an entity and all its descendants.
    /// </summary>
    public static IEnumerable<Entity> SelfAndDescendants(Entity entity)
    {
        return entity.Yield().BreadthFirst(x => x.Transform.Children.Select(y => y.Entity));
    }

    /// <summary>
    /// Gets how far an entity moves up (positive) or down (negative) so that the bottom of its bounds touches the first
    /// surface below it: the highest top of the boxes under its footprint that is below its center, or the ground plane.
    /// </summary>
    /// <param name="bounds">The world bounds of the entity.</param>
    /// <param name="surfaces">The world bounding boxes of the other models.</param>
    /// <param name="groundHeight">The height of the ground plane.</param>
    /// <returns>The vertical move, or <c>null</c> when nothing is below (the entity is under the ground).</returns>
    public static float? GetFloorOffset(BoundingBox bounds, IEnumerable<BoundingBox> surfaces, float groundHeight = GroundHeight)
    {
        var center = bounds.Center.Y;
        float? floor = groundHeight <= center ? groundHeight : null;
        foreach (var surface in surfaces)
        {
            // Under the footprint of the entity (a point entity counts as a footprint of its own), and below its middle: a
            // floor it sinks into a bit still counts
            if (!OverlapsHorizontally(bounds, surface) || surface.Maximum.Y > center)
                continue;
            if (floor is not { } best || surface.Maximum.Y > best)
                floor = surface.Maximum.Y;
        }
        return floor - bounds.Minimum.Y;
    }

    private static bool OverlapsHorizontally(BoundingBox a, BoundingBox b)
    {
        return a.Minimum.X <= b.Maximum.X && a.Maximum.X >= b.Minimum.X && a.Minimum.Z <= b.Maximum.Z && a.Maximum.Z >= b.Minimum.Z;
    }
}
