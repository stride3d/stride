// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

[DataContract]
public struct EffectResourceBindingDescription
{
    /// <summary>
    /// Describes a shader parameter for a resource type.
    /// </summary>
    public EffectParameterKeyInfo KeyInfo;

    public EffectParameterClass Class;

    public EffectParameterType Type;

    public EffectTypeDescription ElementType;

    public string RawName;

    public string ResourceGroup;

    public ShaderStage Stage;

    public int SlotStart;

    public int SlotCount;

    public string LogicalGroup;


    /// <inheritdoc/>
    public override readonly string ToString()
    {
        if (SlotCount <= 0)
            return $"<Invalid Binding>";

        string stage = Stage != ShaderStage.None ? $"{Stage} " : "";
        string bindingName = KeyInfo.Key is not null && !string.IsNullOrEmpty(RawName)
            ? $" {KeyInfo.Key} -> {RawName}"
            : "";
        string slots = SlotCount == 1
            ? $"(Slot {SlotStart})"
            : $"(Slots {SlotStart} to {SlotStart + SlotCount - 1})";

        return $"Binding [{stage}{Class}{bindingName} {slots}]";
    }
}
