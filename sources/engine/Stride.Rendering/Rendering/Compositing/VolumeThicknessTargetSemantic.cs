// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// A render target of the volume thickness stage; its pass shader writes it itself.
    /// </summary>
    public class VolumeThicknessTargetSemantic : IRenderTargetSemantic
    {
        /// <inheritdoc/>
        public ShaderSource ShaderClass { get; } = new ShaderClassSource("ComputeColor");
    }
}
