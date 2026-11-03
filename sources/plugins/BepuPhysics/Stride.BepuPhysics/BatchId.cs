// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics;

/// <summary>
/// Provides a unique id for custom bepu constraints and processors.
/// </summary>
/// <inheritdoc cref="BatchId{T}.Id"/>
public static class BatchId<T>
{
    // Readonly to help JIT inline the field into its runtime constant representation
    /// <summary>
    /// Returns a fixed unique int for <typeparamref name="T"/>
    /// </summary>
    /// <remarks>
    /// A custom constraint or processor that does not rely on <see cref="Id"/> will break all uniqueness contract established here. <br/><br/>
    ///
    /// The mapping between id and their associated type varies between sessions,
    /// you cannot rely on this value being consistent across runs. <br/><br/>
    ///
    /// If a project you have no control over doesn't go through this field,
    /// you must shift <see cref="BatchIdStore.LatestId"/> past the ids that project registered before physics initialization to avoid issues.
    /// </remarks>
    public static readonly int Id = Interlocked.Increment(ref BatchIdStore.LatestId);
}

public static class BatchIdStore
{
    /// <summary>
    /// Low-level access to the backing store, go through <see cref="BatchId{T}"/> instead
    /// </summary>
    /// <remarks>
    /// If a project you have no control over doesn't go through <see cref="BatchId{T}.Id"/>,
    /// you should shift this value past the ids that project registered before physics initialization to avoid issues
    /// </remarks>
    public static int LatestId = 60; // 55 Ids used in bepu as of writing, we'll start a bit away just to be safe
}
