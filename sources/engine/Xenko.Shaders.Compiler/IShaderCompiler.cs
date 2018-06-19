// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Diagnostics;
using Xenko.Rendering;
using Xenko.Graphics;

namespace Xenko.Shaders.Compiler
{
    internal class ShaderBytecodeResult : LoggerResult
    {
        public ShaderBytecode Bytecode { get; set; }

        public string DisassembleText { get; set; }
    }

    internal interface IShaderCompiler
    {
        ShaderBytecodeResult Compile(string shaderSource, string entryPoint, ShaderStage stage, EffectCompilerParameters effectParameters, EffectReflection reflection, string sourceFilename = null);
    }
}
