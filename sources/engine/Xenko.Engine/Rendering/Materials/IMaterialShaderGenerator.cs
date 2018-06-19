// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Defines the interface to generate the shaders for a <see cref="IMaterialFeature"/>
    /// </summary>
    public interface IMaterialShaderGenerator
    {
        /// <summary>
        /// Generates the shader.
        /// </summary>
        /// <param name="context">The context.</param>
        void Visit(MaterialGeneratorContext context);
    }
}
