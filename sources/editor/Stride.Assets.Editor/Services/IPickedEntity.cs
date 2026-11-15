// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// An entity, and optionally one of its components, picked with an <see cref="IEntityPickerService"/>.
/// </summary>
public interface IPickedEntity
{
    /// <summary>
    /// Gets the picked entity.
    /// </summary>
    EntityViewModel? Entity { get; }

    /// <summary>
    /// Gets the index of the picked component in the components of <see cref="Entity"/>, or -1 when no component was picked.
    /// </summary>
    int ComponentIndex { get; }
}
