// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;

namespace Stride.Graphics.Tests
{
    /// <summary>
    ///   A typed Buffer with unordered access (<c>RWBuffer&lt;T&gt;</c>) on Direct3D 11, whose typed UAVs start at feature level 11_0.
    /// </summary>
    public class TestBufferTypedUnorderedAccess
    {
        [SkippableFact]
        public void BelowLevel11TheErrorNamesTheProfile()
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D11, "Feature levels are Direct3D 11's");

            using var device = GraphicsDevice.New(DeviceCreationFlags.None, GraphicsProfile.Level_10_0);
            var error = Assert.Throws<NotSupportedException>(() => Buffer.Typed.New(device, 16, PixelFormat.R32_UInt, unorderedAccess: true));
            Assert.Contains(nameof(GraphicsProfile.Level_11_0), error.Message);

            // Structured and raw buffers keep their unordered access below 11_0.
            using var structured = Buffer.Structured.New<uint>(device, 16, unorderedAccess: true);
            using var raw = Buffer.Raw.New(device, 64, BufferFlags.UnorderedAccess);
        }

        [SkippableFact]
        public void AtLevel11TheBufferIsCreated()
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D11, "Feature levels are Direct3D 11's");

            using var device = GraphicsDevice.New(DeviceCreationFlags.None, GraphicsProfile.Level_11_0);
            using var buffer = Buffer.Typed.New(device, 16, PixelFormat.R32_UInt, unorderedAccess: true);
            Assert.NotNull(buffer);
        }
    }
}
