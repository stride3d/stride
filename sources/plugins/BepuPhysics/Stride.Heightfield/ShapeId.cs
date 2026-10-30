// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Having it here is really stupid, but
// - We can't move it to Stride's bepu because this assembly does not reference it,
//   it's the other way around
// - This assembly, and user assemblies need this type
// - It doesn't make sense to move it outside of those two assemblies

using BepuPhysics.Collidables;

namespace Stride.BepuPhysics;

/// <summary>
/// Provides a non-colliding unique id for a custom shape,
/// cannot be guaranteed for shapes whose Ids are not retrieved through this type.
/// </summary>
public struct ShapeId<T>
{
    // Readonly to help JIT inline the field into its runtime constant representation
    /// <summary>
    /// Returns a fixed unique int for <typeparamref name="T"/>
    /// </summary>
    public static readonly int Id = Interlocked.Increment(ref ShapeIdStore.LatestId);
}

public static class ShapeIdStore
{
    /// <summary>
    /// Low-level access to the backing store, go through <see cref="ShapeId{T}"/> instead
    /// </summary>
    public static int LatestId = Mesh.Id; // Last known Id allocated by bepu as of writing
}
