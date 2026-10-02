// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;
using Stride.Rendering.Materials;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// The runtime representation of a compiled <see cref="IHeightfieldSource"/>
/// </summary>
[DataContract("Heightfield")]
[ContentSerializer(typeof(DataContentSerializerWithReuse<Heightfield>))]
[ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Heightfield>), Profile = "Content")]
[DataSerializerGlobal(typeof(CloneSerializer<Heightfield>), Profile = "Clone")]
public class Heightfield : IHeightfieldPhysicsSource, IHeightfieldRenderingSource
{
    /// <inheritdoc cref="IHeightfieldSource.Size" />
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    public float Size
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    } = 1024;

    /// <inheritdoc cref="IHeightfieldSource.Subdivision" />
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    public int Subdivision
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    } = 2048;

    /// <inheritdoc cref="HeightfieldShape.CoarseBlocksSubdivision"/>
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    public int CoarseBlockSubdivision
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    }

    /// <inheritdoc cref="HeightfieldShape.CoarseBlocksAddress" />
    [MemberRequired]
    public HeightRange[] CoarseBlocks { get; set; } = null!;

    /// <inheritdoc cref="HeightfieldShape.MinHeight"/>
    public float MinHeight { get; set; }

    /// <inheritdoc cref="HeightfieldShape.MaxHeight"/>
    public float MaxHeight { get; set; }

    /// <summary>
    /// The heightfield function for the rendering and physics-side
    /// </summary>
    [MemberRequired]
    public IHeightfieldRuntimeLayer TopmostLayer { get; set; } = null!;

    /// <inheritdoc cref="IHeightfieldPhysicsSource.GetColliderData" />
    public void GetColliderData(out IHeightfieldSampler sampler, out HeightRange[] coarseBlocks, out int coarseBlocksSubdivision)
    {
        sampler = TopmostLayer.BuildHeightfieldFunction();
        coarseBlocksSubdivision = CoarseBlockSubdivision;
        coarseBlocks = CoarseBlocks;
    }

    /// <inheritdoc cref="IHeightfieldRenderingSource.BuildGpuSideSampler" />
    public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context)
    {
        return TopmostLayer.BuildGpuSideSampler(context);
    }
}
