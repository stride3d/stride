// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering.Materials;

namespace Stride.BepuPhysics.Definitions.Heightfield;

/// <summary>
/// The definition for an <see cref="HeightfieldShape"/>
/// </summary>
/// <example>
/// See Stride.BepuPhysics.Definitions.Heightfield.Assets.Heightfield in Stride.Assets for an example implementation
/// </example>
public interface IHeightfieldSource
{
    /// <summary>
    /// How large the heightfield is in units, 10 would occupy an area of 10^2 units
    /// </summary>
    float Size { get; }

    /// <inheritdoc cref="HeightfieldShape.Subdivision"/>
    int Subdivision { get; }

    /// <inheritdoc cref="HeightfieldShape.MinHeight"/>
    float MinHeight { get; }

    /// <inheritdoc cref="HeightfieldShape.MaxHeight"/>
    float MaxHeight { get; }

    /// <summary>
    /// Retrieve the physics-side collision data
    /// </summary>
    /// <param name="sampler">The datatype to call when querying height data, see <see cref="IHeightfieldSampler"/></param>
    /// <param name="coarseBlocks">The sub-bounding boxes of this heightfield, any writes to this array after the call is replicated on the physics shape</param>
    /// <param name="coarseBlocksSubdivision">
    /// Amount of blocks along one axis. <paramref name="coarseBlocks"/> holds <paramref name="coarseBlocksSubdivision"/>^2 blocks.
    /// </param>
    void GetColliderData(out IHeightfieldSampler sampler, out HeightRange[] coarseBlocks, out int coarseBlocksSubdivision);

    /// <summary>
    /// Returns the material-side displacement matching what <see cref="GetColliderData"/> would provide on the physics-side
    /// </summary>
    IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context);
}
