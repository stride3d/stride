// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// Arguments of the events raised for an element of an entity hierarchy (the root or an entity).
/// </summary>
public sealed class EntityHierarchyElementEventArgs : EventArgs
{
    public EntityHierarchyElementEventArgs(EntityHierarchyElementViewModel element)
    {
        Element = element;
    }

    /// <summary>
    /// Gets the element.
    /// </summary>
    public EntityHierarchyElementViewModel Element { get; }
}
