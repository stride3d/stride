// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// Result of an effect bytecode compilation.
    /// </summary>
    public struct EffectBytecodeCompilerResult
    {
        private static readonly LoggerResult EmptyLoggerResult = new LoggerResult();

        /// <summary>
        /// The effect bytecode. Might be null.
        /// </summary>
        public readonly EffectBytecode Bytecode;

        /// <summary>
        /// The compilation log.
        /// </summary>
        public readonly LoggerResult CompilationLog;

        /// <summary>
        /// Gets or sets a value that specifies how the shader was loaded.
        /// </summary>
        public readonly EffectBytecodeCacheLoadSource LoadSource;

        public EffectBytecodeCompilerResult(EffectBytecode bytecode, EffectBytecodeCacheLoadSource loadSource) : this()
        {
            Bytecode = bytecode;
            CompilationLog = EmptyLoggerResult;
            LoadSource = loadSource;
        }

        public EffectBytecodeCompilerResult(EffectBytecode bytecode, LoggerResult compilationLog)
        {
            Bytecode = bytecode;
            CompilationLog = compilationLog;
            LoadSource = EffectBytecodeCacheLoadSource.JustCompiled;
        }
    }
}
