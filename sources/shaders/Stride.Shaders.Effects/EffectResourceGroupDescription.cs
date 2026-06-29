// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Stride.Core;

namespace Stride.Shaders;

/// <summary>
///   Describes a resource group (descriptor set) and all its resource entries.
///   Entries are ordered: ConstantBuffer first (if present), then textures/samplers/UAVs.
/// </summary>
[DataContract]
public class EffectResourceGroupDescription
{
    /// <summary>
    ///   The resource group name (e.g. "PerFrame", "PerView", "PerDraw", "PerMaterial", "Globals").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///   The constant buffer description for this group, or null if the group has no constant buffer.
    /// </summary>
    public EffectConstantBufferDescription ConstantBuffer { get; set; }

    /// <summary>
    ///   All resource entries in this group, ordered: ConstantBuffer entry first, then other resources.
    /// </summary>
    public List<EffectResourceEntry> Entries { get; set; } = [];

    /// <inheritdoc/>
    public override string ToString()
    {
        var cb = ConstantBuffer != null ? $", cbuffer: {ConstantBuffer.Name} [{ConstantBuffer.Size} bytes]" : "";
        return $"ResourceGroup '{Name}' ({Entries.Count} entries{cb})";
    }
}
