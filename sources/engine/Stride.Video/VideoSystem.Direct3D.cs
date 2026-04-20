// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
#pragma warning disable CA1416 // Validate platform compatibility (no need, we check for Windows already at a higher level)

using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Stride.Games;
using Windows.Win32.Media.MediaFoundation;
using static Windows.Win32.PInvoke;
using IUnknown = Windows.Win32.System.Com.IUnknown;

namespace Stride.Video;

public unsafe partial class VideoSystem
{
    internal IMFDXGIDeviceManager* DxgiDeviceManager;

    public override unsafe void Initialize()
    {
        base.Initialize();

        var graphicsDevice = Services.GetService<IGame>().GraphicsDevice;

        var d3d11Device = graphicsDevice.NativeDevice;

        IMFDXGIDeviceManager* manager;
        MFCreateDXGIDeviceManager(out var resetToken, &manager).ThrowOnFailure();
        DxgiDeviceManager = manager;
        DxgiDeviceManager->ResetDevice((IUnknown*)(d3d11Device.Handle), resetToken);

        // Add multi-thread protection on the device
        HResult result = d3d11Device.QueryInterface(out ComPtr<ID3D11Multithread> multiThread);

        if (result.IsFailure)
            result.Throw();

        multiThread.SetMultithreadProtected(true);
        multiThread.Dispose();

        MFStartup(MF_VERSION, 0).ThrowOnFailure();
    }

    protected override void Destroy()
    {
        DxgiDeviceManager->Release();
        MFShutdown().ThrowOnFailure();

        base.Destroy();
    }
}

#endif
