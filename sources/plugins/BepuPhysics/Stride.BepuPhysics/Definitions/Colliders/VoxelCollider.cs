// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Definitions.Colliders.Voxels;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Colliders;

/// <summary>
/// A <see cref="VoxelColliderBase{TSource}"/> over a copy of float samples, laid out x-major with z varying fastest.
/// </summary>
[DataContract]
[NonInstantiable]
public sealed unsafe class VoxelCollider : VoxelColliderBase<FloatVoxelSource>, IDisposable
{
    /// <summary>Fewest samples per axis a grid has: two, which is one cell.</summary>
    private const int MinSamplesPerAxis = 2;

    private float* _samples;
    private int _samplesX, _samplesY, _samplesZ;

    /// <summary>Gets the number of samples on each axis, one more than the number of cells.</summary>
    [DataMemberIgnore]
    public Int3 SampleCount => new(_samplesX, _samplesY, _samplesZ);

    /// <summary>Gets whether a field has been supplied with <see cref="SetData"/>.</summary>
    [DataMemberIgnore]
    public bool HasData => _samples != null;

    /// <summary>Copies a density field into the collider.</summary>
    /// <param name="samplesX">Samples on the X axis.</param>
    /// <param name="samplesY">Samples on the Y axis.</param>
    /// <param name="samplesZ">Samples on the Z axis.</param>
    /// <param name="samples">The densities, x-major with z varying fastest.</param>
    /// <exception cref="ArgumentOutOfRangeException">An axis has fewer than two samples.</exception>
    /// <exception cref="ArgumentException"><paramref name="samples"/> is shorter than the grid.</exception>
    public void SetData(int samplesX, int samplesY, int samplesZ, ReadOnlySpan<float> samples)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(samplesX, MinSamplesPerAxis);
        ArgumentOutOfRangeException.ThrowIfLessThan(samplesY, MinSamplesPerAxis);
        ArgumentOutOfRangeException.ThrowIfLessThan(samplesZ, MinSamplesPerAxis);
        var count = samplesX * samplesY * samplesZ;
        if (samples.Length < count)
            throw new ArgumentException($"A {samplesX}x{samplesY}x{samplesZ} grid needs {count} samples, got {samples.Length}.", nameof(samples));

        var resized = samplesX != _samplesX || samplesY != _samplesY || samplesZ != _samplesZ;
        if (resized)
        {
            FreeSamples();
            _samples = (float*)NativeMemory.Alloc((nuint)count, sizeof(float));
            _samplesX = samplesX;
            _samplesY = samplesY;
            _samplesZ = samplesZ;
        }
        samples[..count].CopyTo(new Span<float>(_samples, count));
        if (resized)
            NotifyFieldChanged();
    }

    /// <summary>Overwrites one sample; the simulation reads it on its next step without rebuilding anything.</summary>
    /// <remarks>Call it between simulation steps. Debug views keep a copy of the surface until <see cref="VoxelColliderBase{TSource}.NotifyFieldChanged"/>.</remarks>
    /// <param name="x">Sample index on the X axis.</param>
    /// <param name="y">Sample index on the Y axis.</param>
    /// <param name="z">Sample index on the Z axis.</param>
    /// <param name="density">The new density.</param>
    /// <exception cref="InvalidOperationException">No field was supplied yet.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The sample is outside the grid.</exception>
    public void SetVoxel(int x, int y, int z, float density) => _samples[SampleIndex(x, y, z)] = density;

    /// <summary>Reads one sample.</summary>
    /// <param name="x">Sample index on the X axis.</param>
    /// <param name="y">Sample index on the Y axis.</param>
    /// <param name="z">Sample index on the Z axis.</param>
    /// <returns>The density of the sample.</returns>
    /// <exception cref="InvalidOperationException">No field was supplied yet.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The sample is outside the grid.</exception>
    public float GetVoxel(int x, int y, int z) => _samples[SampleIndex(x, y, z)];

    /// <summary>Frees the field and detaches the collider from the simulation.</summary>
    public void Dispose()
    {
        FreeSamples();
        NotifyFieldChanged();
        GC.SuppressFinalize(this);
    }

    ~VoxelCollider() => FreeSamples();

    /// <inheritdoc/>
    protected override bool TryGetSource(out FloatVoxelSource source)
    {
        source = default;
        if (_samples == null)
            return false;
        source = new FloatVoxelSource
        {
            Samples = new Buffer<float>(_samples, _samplesX * _samplesY * _samplesZ),
            SamplesX = _samplesX,
            SamplesY = _samplesY,
            SamplesZ = _samplesZ,
        };
        return true;
    }

    private void FreeSamples()
    {
        if (_samples == null)
            return;
        NativeMemory.Free(_samples);
        _samples = null;
        _samplesX = _samplesY = _samplesZ = 0;
    }

    private int SampleIndex(int x, int y, int z)
    {
        if (_samples == null)
            throw new InvalidOperationException($"This {nameof(VoxelCollider)} has no field; call {nameof(SetData)} first.");
        if ((uint)x >= (uint)_samplesX || (uint)y >= (uint)_samplesY || (uint)z >= (uint)_samplesZ)
            throw new ArgumentOutOfRangeException(nameof(x), $"({x}, {y}, {z}) is outside a {_samplesX}x{_samplesY}x{_samplesZ} grid.");
        return (x * _samplesY + y) * _samplesZ + z;
    }
}
