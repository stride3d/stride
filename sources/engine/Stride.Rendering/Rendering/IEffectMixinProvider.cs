// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering
{
    /// <summary>
    /// Defines the interface to provide an effect mixin for a <see cref="CameraRendererMode"/>.
    /// </summary>
    public interface IEffectMixinProvider
    {
        /// <summary>
        /// Generates the shader source used for rendering.
        /// </summary>
        /// <returns>ShaderSource.</returns>
        ShaderSource GenerateShaderSource();
    }

    [DataContract("DefaultEffectMixinProvider")]
    public class DefaultEffectMixinProvider : IEffectMixinProvider
    {
        private readonly ShaderSource shaderSource;

        public DefaultEffectMixinProvider(string name)
        {
            shaderSource = new ShaderClassSource(name);
        }

        public ShaderSource GenerateShaderSource()
        {
            return shaderSource;
        }
    }
}
