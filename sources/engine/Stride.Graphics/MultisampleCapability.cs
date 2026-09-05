// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Multisampled rendering of one pixel format at one sample count.
/// </summary>
/// <remarks>
///   Multisampling is graded and it is per format, so a renderer declares the format it renders and the
///   count it wants. The device answers with the highest count it supports for that format.
/// </remarks>
internal sealed class MultisampleCapability : GraphicsCapability
{
    private readonly PixelFormat format;
    private readonly MultisampleCount count;

    internal MultisampleCapability(PixelFormat format, MultisampleCount count)
    {
        this.format = format;
        this.count = count;
    }

    /// <inheritdoc/>
    public override GraphicsCapabilityKind Kind => GraphicsCapabilityKind.Multisampling;

    /// <inheritdoc/>
    public override string Name => $"multisampling at {(int) count} samples on {format}";

    /// <inheritdoc/>
    public override bool IsProvidedByDevice(in GraphicsDeviceFeatures features) =>
        features[format].MultisampleCountMax >= count;
}
