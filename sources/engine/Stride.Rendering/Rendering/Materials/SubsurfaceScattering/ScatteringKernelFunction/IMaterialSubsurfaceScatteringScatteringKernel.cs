// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Common interface for calculating the scattering profile applied during the forward pass using the subsurface scattering shading model.
    /// </summary>
    public interface IMaterialSubsurfaceScatteringScatteringKernel
    {
        /// <summary>
        /// Generates the scattering kernel that is fed into the SSS post-process.
        /// </summary>
        /// <returns>ShaderSource.</returns>
        Vector4[] Generate();
    }
}
