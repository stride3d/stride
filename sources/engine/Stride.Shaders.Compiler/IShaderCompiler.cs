// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Shaders.Compiler;

/// <summary>
///   Provides functionality to compile Shader source code into bytecode for various Shader stages,
///   and handles Shader reflection to provide metadata about the compiled Shaders.
/// </summary>
internal interface IShaderCompiler
{
    /// <summary>
    ///   Compiles the specified Shader source code into byte-code for a given shader stage.
    /// </summary>
    /// <param name="shaderSource">The source code of the Shader to compile.</param>
    /// <param name="entryPoint">The entry point function name within the Shader source.</param>
    /// <param name="stage">
    ///   The Shader stage for which the byte-code is being compiled
    ///   (e.g., <see cref="ShaderStage.Vertex"/>, <see cref="ShaderStage.Pixel"/>).
    /// </param>
    /// <param name="effectParameters">
    ///   A set of parameters that influence the compilation process, such as debug and optimization settings.
    /// </param>
    /// <param name="reflection">An object to be updated with reflection data from the compiled Shader.</param>
    /// <param name="sourceFilename">The optional filename of the Shader source.</param>
    /// <returns>
    ///   A <see cref="ShaderBytecodeResult"/> containing the compiled Shader byte-code and any warnings or errors
    ///   encountered during compilation.
    /// </returns>
    /// <exception cref="ArgumentException">The specified Shader <paramref name="stage"/> is not supported.</exception>
    /// <exception cref="ArgumentException">
    ///   The specified <see cref="EffectCompilerParameters.Profile"/> in <paramref name="effectParameters"/> is not supported.
    /// </exception>
    /// <exception cref="NotImplementedException">
    ///   During reflection, if an unsupported <see cref="EffectParameterClass"/> or <see cref="EffectParameterType"/>
    ///   is encountered.
    /// </exception>
    ShaderBytecodeResult Compile(string shaderSource, string entryPoint, ShaderStage stage,
                                 EffectCompilerParameters effectParameters, EffectReflection reflection,
                                 string? sourceFilename = null);
}
