// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using SharpDX.MediaFoundation;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Stride.Games;

namespace Stride.Video;

public partial class VideoSystem
{
    public DXGIDeviceManager DxgiDeviceManager;   // TODO: Remove when Silk includes Media Foundation

    public override unsafe void Initialize()
    {
        base.Initialize();

        var graphicsDevice = Services.GetService<IGame>().GraphicsDevice;

        var d3d11Device = graphicsDevice.NativeDevice;
        var d3d11DeviceSharpDX = new SharpDX.ComObject((IntPtr) d3d11Device.Handle);

        DxgiDeviceManager = new DXGIDeviceManager();
        DxgiDeviceManager.ResetDevice(d3d11DeviceSharpDX);

        // Add multi-thread protection on the device
        HResult result = d3d11Device.QueryInterface(out ComPtr<ID3D11Multithread> multiThread);

        if (result.IsFailure)
            result.Throw();

        multiThread.SetMultithreadProtected(true);
        multiThread.Dispose();

        MediaManager.Startup();
    }
}

#endif
