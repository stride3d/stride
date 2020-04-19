// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Rendering;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// Main interface used to compile a shader.
    /// </summary>
    public interface IEffectCompiler : IDisposable
    {
        /// <summary>
        /// Compiles the specified shader source.
        /// </summary>
        /// <param name="shaderSource">The shader source.</param>
        /// <param name="compilerParameters">The compiler parameters.</param>
        /// <returns>Result of the compilation.</returns>
        CompilerResults Compile(ShaderSource shaderSource, CompilerParameters compilerParameters);
    }
}
