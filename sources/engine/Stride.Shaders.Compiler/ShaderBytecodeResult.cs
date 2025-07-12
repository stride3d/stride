// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Compiler;

internal class ShaderBytecodeResult : LoggerResult
{
    public ShaderBytecode? Bytecode { get; set; }

    public string? DisassembleText { get; set; }
}
