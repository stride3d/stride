// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12 || STRIDE_GRAPHICS_API_VULKAN

using System;

using Xunit;

namespace Stride.Graphics.Tests
{
    /// <summary>
    ///   The reason the adapter selection uses to skip an adapter Stride can't create a device on.
    /// </summary>
    public class TestAdapterUnsupportedReason
    {
        [Fact]
        public void TheReasonAgreesWithTheDevice()
        {
            foreach (var adapter in GraphicsAdapterFactory.Adapters)
            {
                if (adapter.UnsupportedReason is null)
                {
                    using var device = GraphicsDevice.New(adapter, DeviceCreationFlags.None, GraphicsProfile.Level_11_0);
                }
                else
                {
                    Assert.ThrowsAny<Exception>(() => GraphicsDevice.New(adapter, DeviceCreationFlags.None, GraphicsProfile.Level_11_0).Dispose());
                }
            }
        }

        [Fact]
        public void TheDefaultAndSoftwareAdaptersAreSupported()
        {
            Assert.Null(GraphicsAdapterFactory.DefaultAdapter.UnsupportedReason);

            foreach (var adapter in GraphicsAdapterFactory.Adapters)
            {
                if (adapter.IsSoftwareAdapter)
                    Assert.Null(adapter.UnsupportedReason);
            }
        }
    }
}

#endif
