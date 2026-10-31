// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics;

/// <summary>
/// Provides a non-colliding unique id for a custom shape,
/// cannot be guaranteed for shapes whose Ids are not retrieved through this type.
/// </summary>
public struct BatchId<T>
{
    // Readonly to help JIT inline the field into its runtime constant representation
    /// <summary>
    /// Returns a fixed unique int for <typeparamref name="T"/>
    /// </summary>
    public static readonly int Id = Interlocked.Increment(ref BatchIdStore.LatestId);
}

public static class BatchIdStore
{
    /// <summary>
    /// Low-level access to the backing store, go through <see cref="BatchId{T}"/> instead
    /// </summary>
    public static int LatestId = 60; // 55 Ids used in bepu as of writing, we'll start a bit away just to be safe
}
