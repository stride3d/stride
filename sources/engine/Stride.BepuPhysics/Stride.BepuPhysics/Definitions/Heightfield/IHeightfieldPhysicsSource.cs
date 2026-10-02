// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions.Heightfield;

/// <inheritdoc/>
public interface IHeightfieldPhysicsSource : IHeightfieldSource
{
    /// <summary>
    /// Retrieve the physics-side collision data
    /// </summary>
    /// <param name="sampler">The datatype to call when querying height data, see <see cref="IHeightfieldSampler"/></param>
    /// <param name="coarseBlocks">The sub-bounding boxes of this heightfield, any writes to this array after the call is replicated on the physics shape</param>
    /// <param name="coarseBlocksSubdivision">
    /// Amount of blocks along one axis. <paramref name="coarseBlocks"/> holds <paramref name="coarseBlocksSubdivision"/>^2 blocks.
    /// </param>
    void GetColliderData(out IHeightfieldSampler sampler, out HeightRange[] coarseBlocks, out int coarseBlocksSubdivision);
}
