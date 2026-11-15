// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;
using Stride.Rendering.Materials;

namespace Stride.Heightfield;

/// <summary>
/// The runtime representation of a compiled <see cref="IHeightfieldSource"/>
/// </summary>
[DataContract("Heightfield")]
[ContentSerializer(typeof(DataContentSerializerWithReuse<Heightfield>))]
[DataSerializerGlobal(typeof(CloneSerializer<Heightfield>), Profile = "Clone")]
[ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Heightfield>), Profile = "Content")]
public class Heightfield : IHeightfieldPhysicsSource, IHeightfieldRenderingSource
{
    /// <inheritdoc cref="IHeightfieldSource.Size" />
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    [DataMemberRange(minimum: float.Epsilon)]
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
    /// <exception cref="ArgumentOutOfRangeException">When value is less than two</exception>
    [DataMemberRange(minimum: 2)]
    public int Subdivision
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 2);
            field = value;
        }
    } = 2048;

    /// <inheritdoc cref="IHeightfieldSource.MinHeight"/>
    public float MinHeight { get; set; }

    /// <inheritdoc cref="IHeightfieldSource.MaxHeight"/>
    public float MaxHeight { get; set; }

    /// <summary>
    /// Amount of coarse blocks along one axis.
    /// <see cref="CoarseBlocks"/> holds <see cref="CoarseBlocksSubdivision"/>^2 blocks.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    [DataMemberRange(minimum: 1)]
    public int CoarseBlocksSubdivision
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    }

    /// <summary>
    /// The amount of <see cref="Subdivision"/> along one axis per blocks.
    /// Each block overlaps <see cref="CoarseBlockInterval"/>^2 height samples.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When value is less than or equal to zero</exception>
    [DataMemberRange(minimum: 1)]
    public int CoarseBlockInterval
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
        }
    }

    /// <summary>
    /// <see cref="MinHeight"/> and <see cref="MaxHeight"/> define the bounding box of this heightfield,
    /// and these coarse blocks define sub-bounding boxes that encompass <see cref="CoarseBlocksSubdivision"/>^2 heightfield points.
    /// They are laid out in [x + y * <see cref="CoarseBlocksSubdivision"/>]
    /// and may extend slightly farther than the heightfield points when <see cref="Subdivision"/> is
    /// not evenly divisible by <see cref="CoarseBlocksSubdivision"/>
    /// </summary>
    [MemberRequired]
    public HeightRange[] CoarseBlocks { get; set; } = null!;

    /// <summary>
    /// The heightfield function for the rendering and physics-side
    /// </summary>
    [MemberRequired]
    public IHeightfieldRuntimeLayer TopmostLayer { get; set; } = null!;

    /// <inheritdoc cref="IHeightfieldPhysicsSource.GetColliderData" />
    public void GetColliderData(out IHeightfieldSampler sampler, out HeightRange[] coarseBlocks, out int coarseBlocksSubdivision, out int coarseBlockInterval)
    {
        sampler = TopmostLayer.BuildHeightfieldFunction();
        coarseBlocksSubdivision = CoarseBlocksSubdivision;
        coarseBlocks = CoarseBlocks;
        coarseBlockInterval = CoarseBlockInterval;
    }

    /// <inheritdoc cref="IHeightfieldRenderingSource.BuildGpuSideSampler" />
    public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context)
    {
        return TopmostLayer.BuildGpuSideSampler(context);
    }
}
