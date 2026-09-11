// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   A capability the device either provides or does not, with no degree to it.
/// </summary>
internal sealed class DeviceFeatureCapability : GraphicsCapability
{
    internal DeviceFeatureCapability(GraphicsCapabilityKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    /// <inheritdoc/>
    public override GraphicsCapabilityKind Kind { get; }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override bool IsProvidedByDevice(in GraphicsDeviceFeatures features) => Kind switch
    {
        GraphicsCapabilityKind.ComputeShaders => features.HasComputeShaders,
        GraphicsCapabilityKind.DoublePrecision => features.HasDoublePrecision,
        GraphicsCapabilityKind.DepthAsShaderResource => features.HasDepthAsSRV,
        GraphicsCapabilityKind.MultisampleDepthAsShaderResource => features.HasMultiSampleDepthAsSRV,
        GraphicsCapabilityKind.ResourceRenaming => features.HasResourceRenaming,
        GraphicsCapabilityKind.SRgb => features.HasSRgb,
        GraphicsCapabilityKind.Index32Bits => features.HasIndex32Bits,

        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Not a device feature flag.")
    };
}
