// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Xenko.Graphics;
using Xenko.Shaders.Compiler;

namespace Xenko.Rendering
{
    /// <summary>
    /// Extensions for <see cref="EffectSystem"/>
    /// </summary>
    public static class EffectSystemExtensions
    {
        /// <summary>
        /// Creates an effect.
        /// </summary>
        /// <param name="effectSystem">The effect system.</param>
        /// <param name="effectName">Name of the effect.</param>
        /// <returns>A new instance of an effect.</returns>
        public static TaskOrResult<Effect> LoadEffect(this EffectSystem effectSystem, string effectName)
        {
            var compilerParameters = new CompilerParameters();
            return effectSystem.LoadEffect(effectName, compilerParameters);
        }
    }
}
