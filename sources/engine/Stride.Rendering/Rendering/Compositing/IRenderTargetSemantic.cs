// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using Stride.Shaders;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// The usage of a render target
    /// </summary>
    public interface IRenderTargetSemantic
    {
        /// <summary>
        /// The shader class deriving from ComputeColor that is used as a composition to output to the render target
        /// </summary>
        ShaderSource ShaderClass { get; }
    }

    public class ColorTargetSemantic : IRenderTargetSemantic
    {
        public ShaderSource ShaderClass { get; } = null;
    }

    public class NormalTargetSemantic : IRenderTargetSemantic
    {
        public ShaderSource ShaderClass { get; } = new ShaderClassSource("GBufferOutputNormals");
    }

    public class SpecularColorRoughnessTargetSemantic : IRenderTargetSemantic
    {
        public ShaderSource ShaderClass { get; } = new ShaderClassSource("GBufferOutputSpecularColorRoughness");
    }

    public class VelocityTargetSemantic : IRenderTargetSemantic
    {
        public ShaderSource ShaderClass { get; } = new ShaderClassSource("VelocityOutput");
    }

    public class MaterialIndexTargetSemantic : IRenderTargetSemantic
    {
        public ShaderSource ShaderClass { get; } = new ShaderClassSource("GBufferOutputSubsurfaceScatteringMaterialIndex");
    }

    public class OctahedronNormalSpecularColorTargetSemantic : IRenderTargetSemantic
    {
        public ShaderSource ShaderClass { get; } = new ShaderClassSource("GBufferOutputNormalSpec");
    }

    public class EnvironmentLightRoughnessTargetSemantic : IRenderTargetSemantic
    {
        public ShaderSource ShaderClass { get; } = new ShaderClassSource("GBufferOutputIblRoughness");
    }
}
