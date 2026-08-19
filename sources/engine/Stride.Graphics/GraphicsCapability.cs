// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Something a renderer can need from the graphics device, and which the device can answer for itself.
/// </summary>
/// <remarks>
///   A capability owns both its identity and how to read it from <see cref="GraphicsDeviceFeatures"/>,
///   so a renderer states what it needs without also stating whether it has it.
/// </remarks>
public abstract class GraphicsCapability
{
    /// <summary>
    ///   Compute shaders, and unordered access on structured and raw buffers.
    /// </summary>
    public static GraphicsCapability ComputeShaders { get; } =
        new DeviceFeatureCapability(GraphicsCapabilityKind.ComputeShaders, "compute shaders");

    /// <summary>
    ///   Double precision operations in shaders.
    /// </summary>
    public static GraphicsCapability DoublePrecision { get; } =
        new DeviceFeatureCapability(GraphicsCapabilityKind.DoublePrecision, "double precision in shaders");

    /// <summary>
    ///   Reading the depth buffer as a shader resource.
    /// </summary>
    public static GraphicsCapability DepthAsShaderResource { get; } =
        new DeviceFeatureCapability(GraphicsCapabilityKind.DepthAsShaderResource, "depth as a shader resource");

    /// <summary>
    ///   Reading a multisampled depth buffer as a shader resource.
    /// </summary>
    public static GraphicsCapability MultisampleDepthAsShaderResource { get; } =
        new DeviceFeatureCapability(GraphicsCapabilityKind.MultisampleDepthAsShaderResource,
                                    "multisampled depth as a shader resource");

    /// <summary>
    ///   Renaming a resource on map or full update, rather than waiting for the GPU.
    /// </summary>
    public static GraphicsCapability ResourceRenaming { get; } =
        new DeviceFeatureCapability(GraphicsCapabilityKind.ResourceRenaming, "resource renaming");

    /// <summary>
    ///   sRGB textures and render targets.
    /// </summary>
    public static GraphicsCapability SRgb { get; } =
        new DeviceFeatureCapability(GraphicsCapabilityKind.SRgb, "sRGB textures and render targets");

    /// <summary>
    ///   Multisampled rendering of <paramref name="format"/> at <paramref name="count"/> samples.
    /// </summary>
    /// <param name="format">The pixel format to render multisampled.</param>
    /// <param name="count">The sample count the renderer needs.</param>
    public static GraphicsCapability Multisampling(PixelFormat format, MultisampleCount count) =>
        new MultisampleCapability(format, count);

    /// <summary>
    ///   What a backend implements, or does not.
    /// </summary>
    public abstract GraphicsCapabilityKind Kind { get; }

    /// <summary>
    ///   How the report names this capability.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    ///   Whether the device provides this capability.
    /// </summary>
    /// <param name="features">The features of the device to read.</param>
    /// <remarks>
    ///   This answers for the hardware and the driver alone. A backend that has not implemented
    ///   <see cref="Kind"/> cannot use the capability whatever this returns, so read
    ///   <see cref="GraphicsDeviceFeatures.Supports"/> to ask both questions at once.
    /// </remarks>
    public abstract bool IsProvidedByDevice(in GraphicsDeviceFeatures features);

    /// <inheritdoc/>
    public override string ToString() => Name;
}
