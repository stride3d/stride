// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// Where the vertices of a soft body's model are moved to follow its particles.
/// </summary>
public enum SoftBodyDeformationMode
{
    /// <summary>
    /// Only the particles are sent to the GPU each frame, the vertex shader of every pass moves the vertices, normals and tangents.
    /// </summary>
    /// <remarks> Falls back to <see cref="Cpu"/> when the graphics profile is below <see cref="Graphics.GraphicsProfile.Level_11_0"/>. </remarks>
    Gpu,

    /// <summary>
    /// The vertices, normals and tangents are computed on the CPU and the whole vertex buffer is sent to the GPU each frame.
    /// </summary>
    Cpu,
}
