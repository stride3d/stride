// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering.Materials;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// The runtime representation for the layer of an <see cref="Heightfield"/>,
/// prepares an <see cref="IHeightfieldFunction"/> for evaluation by the physics engine at runtime 
/// </summary>
public interface IHeightfieldRuntimeLayer
{
    /// <summary>
    /// Returns the actual function called by the physics engine to evaluate the heights of an <see cref="Heightfield"/>
    /// </summary>
    IHeightfieldFunction BuildHeightfieldFunction();

    /// <summary>
    /// Returns the material-side displacement matching what <see cref="BuildHeightfieldFunction"/> would output for the physics-side
    /// </summary>
    IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context);
}
