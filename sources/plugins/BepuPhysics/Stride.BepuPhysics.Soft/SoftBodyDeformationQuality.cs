// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// How the vertices of the volumetric soft bodies' models follow the particles of their lattice, see
/// <see cref="SoftBodyConfiguration.DeformationQuality"/>. Cloths are not concerned, their vertices are their particles.
/// </summary>
public enum SoftBodyDeformationQuality
{
    /// <summary>
    /// Each particle carries how the lattice is deformed around it, a vertex blends what the eight particles of its cell
    /// expect of it, as the bones of a skinned mesh do. The surface and its shading are smooth across the cells,
    /// for a few more operations per vertex and per particle.
    /// </summary>
    Smooth,

    /// <summary>
    /// A vertex is interpolated between the eight particles of the cell it is in. The cheapest; the surface folds along
    /// the faces of the cells and its shading shows them, the more so the coarser the lattice and the stronger the deformation.
    /// </summary>
    Fast,
}
