// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compiler;

/// <summary>
///   A result class for storing the output of a Shader compilation process, serving also
///   as a logger for any messages generated during the compilation.
/// </summary>
internal class ShaderBytecodeResult : LoggerResult
{
    /// <summary>
    ///   Gets or sets the compiled Shader byte-code if the compilation is succesful.
    /// </summary>
    /// <value>The compiled Shader byte-code, or <see langword="null"/> if the compilation failed.</value>
    public ShaderBytecode? Bytecode { get; set; }

    /// <summary>
    ///   Gets or sets the decompiled Shader HLSL code from the compiled Shader byte-code if the compilation is succesful,
    ///   useful for debugging or analysis purposes.
    /// </summary>
    /// <value>The decompiled Shader HLSL code, or <see langword="null"/> if the compilation failed.</value>
    public string? DisassembleText { get; set; }
}
