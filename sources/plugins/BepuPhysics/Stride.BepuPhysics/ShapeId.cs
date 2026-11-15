// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Collidables;

namespace Stride.BepuPhysics;

/// <summary>
/// Provides unique ids for custom bepu shapes.
/// </summary>
/// <inheritdoc cref="ShapeId{T}.Id"/>
public struct ShapeId<T>
{
    // Readonly to help JIT inline the field into its runtime constant representation
    /// <summary>
    /// Returns a fixed unique int for <typeparamref name="T"/>
    /// </summary>
    /// <remarks>
    /// A custom shape that does not rely on <see cref="Id"/> will break all uniqueness contract established here. <br/><br/>
    ///
    /// The mapping between id and their associated type varies between sessions,
    /// you cannot rely on this value being consistent across runs. <br/><br/>
    ///
    /// If a project you have no control over doesn't go through this field,
    /// you must shift <see cref="ShapeIdStore.LatestId"/> past the ids that project registered before physics initialization to avoid issues.
    /// </remarks>
    public static readonly int Id = Interlocked.Increment(ref ShapeIdStore.LatestId);
}

public static class ShapeIdStore
{
    /// <summary>
    /// Low-level access to the backing store, go through <see cref="ShapeId{T}"/> instead
    /// </summary>
    /// <remarks>
    /// If a project you have no control over doesn't go through <see cref="ShapeId{T}.Id"/>,
    /// you should shift this value past the ids that project registered before physics initialization to avoid issues
    /// </remarks>
    public static int LatestId = Mesh.Id; // Last known Id allocated by bepu as of writing
}
