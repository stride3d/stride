// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Storage;

namespace Stride.Shaders.Compiler;

/// <summary>
///   The work of the HLSL back end that effects share when they give the same SPIR-V, or the same HLSL for a stage.
/// </summary>
/// <remarks>
///   Each key is a hash of the whole input of the step it skips, so an entry is never out of date.
/// </remarks>
internal sealed class HlslBackendCache
{
    /// <summary>
    ///   The HLSL that SPIRV-Cross writes for each stage of a SPIR-V module once legalized, by the hash of the module the mixer gave.
    /// </summary>
    public ConcurrentDictionary<ObjectId, HlslStage[]> HlslStages { get; } = new();

    /// <summary>
    ///   What FXC gives for the HLSL of a stage, by the hash of the HLSL, its entry point, the shader model and the flags.
    /// </summary>
    /// <remarks>
    ///   Only outputs without messages: FXC's messages name the effect.
    /// </remarks>
    public ConcurrentDictionary<ObjectId, FxcBytecode> FxcBytecodes { get; } = new();

    public void Clear()
    {
        HlslStages.Clear();
        FxcBytecodes.Clear();
    }
}

internal sealed record HlslStage(ShaderStage Stage, string EntryPoint, string Code);

internal sealed record FxcBytecode(ObjectId Id, byte[] Data, string? DisassembleText);
