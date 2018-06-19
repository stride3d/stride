// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Rendering;

namespace Xenko.Shaders
{
    /// <summary>
    /// Extension of <see cref="IShaderMixinBuilder"/> that provides keys and mixin informations.
    /// </summary>
    public interface IShaderMixinBuilderExtended : IShaderMixinBuilder
    {
        /// <summary>
        /// Gets an array of <see cref="ParameterKey"/> used by this mixin.
        /// </summary>
        /// <value>The keys.</value>
        ParameterKey[] Keys { get; }

        /// <summary>
        /// Gets the shaders/mixins used by this mixin.
        /// </summary>
        /// <value>The mixins.</value>
        string[] Mixins { get; }
    }
}
