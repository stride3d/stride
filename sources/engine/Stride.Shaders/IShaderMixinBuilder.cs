// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders
{
    /// <summary>
    /// Interface to be implemented for dynamic mixin generation.
    /// </summary>
    public interface IShaderMixinBuilder
    {
        /// <summary>
        /// Generates a mixin.
        /// </summary>
        /// <param name="mixinTree">The mixin tree.</param>
        /// <param name="context">The context.</param>
        void Generate(ShaderMixinSource mixinTree, ShaderMixinContext context);
    }
}
