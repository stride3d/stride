// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compiler;

/// <summary>
///   Result of an Effect bytecode compilation.
/// </summary>
public readonly struct EffectBytecodeCompilerResult
{
    private static readonly LoggerResult EmptyLoggerResult = new();

    /// <summary>
    ///   The Effect bytecode. Can be <see langword="null"/> if no bytecode has been generated.
    /// </summary>
    public readonly EffectBytecode? Bytecode;

    /// <summary>
    ///   The compilation log.
    /// </summary>
    public readonly LoggerResult CompilationLog;

    /// <summary>
    ///   A value indicating how the Effect / Shader was loaded.
    /// </summary>
    public readonly EffectBytecodeCacheLoadSource LoadSource;


    /// <summary>
    ///   Initializes a new instance of the <see cref="EffectBytecodeCompilerResult"/> structure.
    /// </summary>
    /// <param name="bytecode">
    ///   The Effect bytecode. Specify <see langword="null"/> if no bytecode has been generated.
    /// </param>
    /// <param name="loadSource">A value indicating how the Effect / Shader was loaded.</param>
    public EffectBytecodeCompilerResult(EffectBytecode bytecode, EffectBytecodeCacheLoadSource loadSource) : this()
    {
        Bytecode = bytecode;
        CompilationLog = EmptyLoggerResult;
        LoadSource = loadSource;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="EffectBytecodeCompilerResult"/> structure.
    /// </summary>
    /// <param name="bytecode">
    ///   The Effect bytecode. Specify <see langword="null"/> if no bytecode has been generated.
    /// </param>
    /// <param name="compilationLog">
    ///   The log containing information about the compilation, warnings, errors, etc.
    /// </param>
    public EffectBytecodeCompilerResult(EffectBytecode bytecode, LoggerResult compilationLog)
    {
        Bytecode = bytecode;
        CompilationLog = compilationLog;
        LoadSource = EffectBytecodeCacheLoadSource.JustCompiled;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="EffectBytecodeCompilerResult"/> structure.
    /// </summary>
    /// <param name="compilationLog">
    ///   The log containing information about the compilation, warnings, errors, etc.
    /// </param>
    public EffectBytecodeCompilerResult(LoggerResult compilationLog) : this(bytecode: null, compilationLog) { }
}
