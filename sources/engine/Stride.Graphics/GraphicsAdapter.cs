// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    ///   Represents a display subsystem (including one or more GPUs, DACs and video memory).
    ///   A display subsystem is often referred to as a video card, however, on some machines the display subsystem is part of the motherboard.
    /// </summary>
    /// <remarks>
    ///   To enumerate the <see cref="GraphicsAdapter"/>s that are available in the system, see <see cref="GraphicsAdapterFactory"/>.
    /// </remarks>
    public sealed partial class GraphicsAdapter : ComponentBase
    {
        private readonly GraphicsOutput[] graphicsOutputs;

        /// <summary>
        ///   Gets the <see cref="GraphicsOutput"/>s attached to this adapter.
        /// </summary>
        public ReadOnlySpan<GraphicsOutput> Outputs => graphicsOutputs;

        /// <summary>
        ///   Gets the unique identifier of this <see cref="GraphicsAdapter"/>.
        /// </summary>
        public long AdapterUid { get; }

        /// <summary>GPU and driver identification captured at adapter enumeration time.</summary>
        public AdapterDriverInfo DriverInfo { get; private set; }

        /// <summary>
        ///   Returns the description of this <see cref="GraphicsAdapter"/>.
        /// </summary>
        public override string ToString()
        {
            return Description;
        }
    }

    /// <summary>
    ///   GPU / driver / API identification. Populated per-backend.
    /// </summary>
    public sealed class AdapterDriverInfo
    {
        /// <summary>Human-readable GPU name (e.g. "NVIDIA GeForce RTX 5080", "Apple M4", "llvmpipe").</summary>
        public required string GpuName { get; init; }
        /// <summary>PCI vendor ID (e.g. 0x10DE for NVIDIA).</summary>
        public required uint VendorId { get; init; }
        /// <summary>PCI device ID.</summary>
        public required uint DeviceId { get; init; }
        /// <summary>Vendor name mapped from <see cref="VendorId"/> when known (e.g. "NVIDIA", "AMD", "INTEL", "Apple").</summary>
        public string? VendorName { get; init; }
        /// <summary>Driver identity. On Vulkan: <c>VkDriverId</c> enum value (e.g. "MOLTENVK", "MESA_LLVMPIPE"). On D3D: vendor name fallback.</summary>
        public string? DriverId { get; init; }
        /// <summary>Driver display name. On Vulkan: <c>VkPhysicalDeviceDriverProperties.driverName</c>. On D3D: vendor name fallback.</summary>
        public string? DriverName { get; init; }
        /// <summary>Free-form vendor version string. On Vulkan: <c>VkPhysicalDeviceDriverProperties.driverInfo</c> (often includes runtime/build details). Not exposed on D3D.</summary>
        public string? DriverInfo { get; init; }
        /// <summary>Driver version formatted per vendor convention (e.g. "1.4.1" for MoltenVK, "32.0.15.7270" for NVIDIA UMD).</summary>
        public required string DriverVersion { get; init; }
        /// <summary>Graphics API name ("Vulkan", "Direct3D11", "Direct3D12").</summary>
        public required string ApiName { get; init; }
        /// <summary>API-level version (e.g. "1.4.334" for Vulkan, "11_1" for D3D11 feature level).</summary>
        public required string ApiVersion { get; init; }
    }
}
