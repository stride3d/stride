// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// Arguments of the events raised when an entity is added to or removed from an entity hierarchy.
/// </summary>
public sealed class EntityHierarchyChangedEventArgs : EventArgs
{
    public EntityHierarchyChangedEventArgs(EntityHierarchyElementViewModel parent, EntityViewModel entity, int index)
    {
        Parent = parent;
        Entity = entity;
        Index = index;
    }

    /// <summary>
    /// Gets the element the entity was added to or removed from: the root of the hierarchy or another entity, whatever
    /// the folder of the entity.
    /// </summary>
    public EntityHierarchyElementViewModel Parent { get; }

    /// <summary>
    /// Gets the view model of the entity.
    /// </summary>
    public EntityViewModel Entity { get; }

    /// <summary>
    /// Gets the index of the entity in the children of the transform of <see cref="Parent"/>.
    /// </summary>
    public int Index { get; }
}
