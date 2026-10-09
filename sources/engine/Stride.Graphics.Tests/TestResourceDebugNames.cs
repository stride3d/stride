// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12
using System;
using Silk.NET.Direct3D12;
using Xunit;

namespace Stride.Graphics.Tests;

public class TestResourceDebugNames : GraphicTestGameBase
{
    // WKPDID_D3DDebugObjectNameW, where ID3D12Object::SetName stores the name
    private static readonly Guid DebugObjectNameW = new(0x4cca5fd8, 0x921f, 0x42c8, 0x85, 0x66, 0x70, 0xca, 0xf2, 0xa9, 0xb7, 0x41);

    /// <summary>
    /// A name given to a resource after it is created reaches the native object, where debuggers read it.
    /// </summary>
    [Fact]
    public void NameGivenAfterCreationReachesTheNativeObject()
    {
        PerformTest(game =>
        {
            var texture = Texture.New2D(game.GraphicsDevice, 4, 4, PixelFormat.R8G8B8A8_UNorm);
            var buffer = Buffer.Vertex.New(game.GraphicsDevice, 64);

            texture.Name = "RenamedTexture";
            buffer.Name = "RenamedBuffer";

            Assert.Contains("RenamedTexture", GetNativeName(texture));
            Assert.Contains("RenamedBuffer", GetNativeName(buffer));
        });
    }

    private static unsafe string GetNativeName(GraphicsResource resource)
    {
        var guid = DebugObjectNameW;
        uint size = 0;
        resource.NativeDeviceChild.GetPrivateData(ref guid, ref size, null);
        if (size == 0)
            return string.Empty;

        var name = new char[size / sizeof(char)];
        fixed (char* namePointer = name)
            resource.NativeDeviceChild.GetPrivateData(ref guid, ref size, namePointer);
        return new string(name).TrimEnd('\0');
    }
}
#endif
