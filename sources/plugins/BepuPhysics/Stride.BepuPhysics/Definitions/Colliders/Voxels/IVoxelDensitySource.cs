// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using BepuUtilities.Memory;

namespace Stride.BepuPhysics.Definitions.Colliders.Voxels;

/// <summary>
/// Reads the density samples of a voxel field for a <see cref="VoxelColliderBase{TSource}"/>, in whatever layout they are stored.
/// </summary>
/// <remarks>
/// The source is copied into Bepu's shape memory and read from the physics threads: it may only point at unmanaged memory that outlives the collider.
/// </remarks>
public interface IVoxelDensitySource
{
    /// <summary>Gets the number of samples on the X axis.</summary>
    int SamplesX { get; }

    /// <summary>Gets the number of samples on the Y axis.</summary>
    int SamplesY { get; }

    /// <summary>Gets the number of samples on the Z axis.</summary>
    int SamplesZ { get; }

    /// <summary>Reads the density of one sample; the coordinates are always inside the grid.</summary>
    /// <param name="x">Sample index on the X axis.</param>
    /// <param name="y">Sample index on the Y axis.</param>
    /// <param name="z">Sample index on the Z axis.</param>
    /// <returns>The density, solid at or above the collider's iso level.</returns>
    float Density(int x, int y, int z);
}

/// <summary>
/// One <see cref="float"/> per sample, x-major with z varying fastest.
/// </summary>
public struct FloatVoxelSource : IVoxelDensitySource
{
    /// <summary>The samples.</summary>
    public Buffer<float> Samples;

    /// <inheritdoc/>
    public int SamplesX { get; set; }

    /// <inheritdoc/>
    public int SamplesY { get; set; }

    /// <inheritdoc/>
    public int SamplesZ { get; set; }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float Density(int x, int y, int z) => Samples[(x * SamplesY + y) * SamplesZ + z];
}
