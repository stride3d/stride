// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering.Materials;

namespace Stride.Heightfield;

/// <summary>
/// The definition for the <see cref="HeightfieldDisplacementFeature"/>
/// </summary>
/// <example>
/// See Stride.Heightfield.Assets.Heightfield in Stride.Assets for an example implementation
/// </example>
public interface IHeightfieldRenderingSource : IHeightfieldSource
{
    /// <summary>
    /// Returns the material-side displacement matching what <see cref="IHeightfieldPhysicsSource.GetColliderData"/> would provide on the physics-side
    /// </summary>
    IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context);
}
