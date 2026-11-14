// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// Where the vertices of the soft bodies' models are moved to follow their particles, see <see cref="SoftBodyConfiguration.DeformationMode"/>.
/// </summary>
public enum SoftBodyDeformationMode
{
    /// <summary>
    /// The particles are sent to the GPU each frame, with how the lattice is deformed around each of them for the smooth quality;
    /// the vertex shader of every pass moves the vertices, normals and tangents.
    /// </summary>
    /// <remarks>
    /// Falls back to <see cref="Cpu"/> on a device without compute shader support, which comes with the buffers the vertex shader reads,
    /// when the shader profile is below <see cref="Graphics.GraphicsProfile.Level_11_0"/>, or without a graphics compositor to draw with.
    /// </remarks>
    Gpu,

    /// <summary>
    /// The vertices, normals and tangents are computed on the CPU and the whole vertex buffer is sent to the GPU each frame.
    /// </summary>
    Cpu,
}
