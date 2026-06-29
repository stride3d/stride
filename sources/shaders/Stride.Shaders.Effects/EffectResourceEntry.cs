// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Graphics;

namespace Stride.Shaders;

/// <summary>
///   Describes a single resource entry within a resource group.
///   Replaces the per-stage duplicated <see cref="EffectResourceBindingDescription"/> approach
///   with a single entry per resource using <see cref="ShaderStageFlags"/> to indicate active stages.
/// </summary>
[DataContract]
public struct EffectResourceEntry
{
    public EffectResourceEntry(in EffectResourceBindingDescription binding, SamplerStateDescription? samplerState = null)
    {
        KeyInfo = binding.KeyInfo;
        Class = binding.Class;
        Type = binding.Type;
        ElementType = binding.ElementType;
        RawName = binding.RawName;
        Stages = ShaderStageFlags.None;
        SlotStart = binding.SlotStart;
        SlotCount = binding.SlotCount;
        LogicalGroup = binding.LogicalGroup;
        SamplerStateDescription = samplerState;
    }

    public EffectParameterKeyInfo KeyInfo;

    public EffectParameterClass Class;

    public EffectParameterType Type;

    public EffectTypeDescription ElementType;

    public string RawName;

    /// <summary>
    ///   Which shader stages use this resource (bitfield).
    ///   <see cref="ShaderStageFlags.None"/> means the resource is declared but unused by any compiled stage.
    /// </summary>
    public ShaderStageFlags Stages;

    /// <summary>
    ///   The binding slot for this resource. Same across all stages (enforced by the EffectCompiler).
    /// </summary>
    public int SlotStart;

    public int SlotCount;

    public string LogicalGroup;

    /// <summary>
    ///   For sampler resources, the immutable sampler state description. Null for non-sampler resources.
    /// </summary>
    public SamplerStateDescription? SamplerStateDescription;

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        var stages = Stages != ShaderStageFlags.None ? $", stages: {Stages}" : "";
        var name = KeyInfo.Key?.Name ?? KeyInfo.KeyName ?? RawName ?? "<unknown>";
        return $"{name} ({Class} {Type}, slot {SlotStart}{stages})";
    }
}
