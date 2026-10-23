// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
#pragma warning disable CA1416 // Validate platform compatibility

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Stride.Core;
using Stride.Games;
using Stride.Graphics;
using Windows.Win32.Media.MediaFoundation;
using static Windows.Win32.PInvoke;
using IUnknown = Windows.Win32.System.Com.IUnknown;

namespace Stride.Video.Backends;

public sealed unsafe class MediaEngineVideoBackendFactory : VideoBackendFactory
{
    internal IMFDXGIDeviceManager* DxgiDeviceManager;

    public override string Name => "MediaEngine";
    public override int Priority => 200; // prefer over FFmpeg on Windows D3D11 (hardware-accelerated decode)
    public override bool IsSupported(GraphicsDevice device) => device != null;

    public override void InitializeSystem(VideoSystem system)
    {
        var graphicsDevice = system.Services.GetService<IGame>().GraphicsDevice;
        var d3d11Device = graphicsDevice.NativeDevice;

        IMFDXGIDeviceManager* manager;
        MFCreateDXGIDeviceManager(out var resetToken, &manager).ThrowOnFailure();
        DxgiDeviceManager = manager;
        DxgiDeviceManager->ResetDevice((IUnknown*)(d3d11Device.Handle), resetToken);

        HResult hr = d3d11Device.QueryInterface(out ComPtr<ID3D11Multithread> multiThread);
        if (hr.IsFailure)
            hr.Throw();
        multiThread.SetMultithreadProtected(true);
        multiThread.Dispose();

        MFStartup(MF_VERSION, 0).ThrowOnFailure();
    }

    public override void DestroySystem(VideoSystem system)
    {
        if (DxgiDeviceManager != null)
            DxgiDeviceManager->Release();
        DxgiDeviceManager = null;
        MFShutdown().ThrowOnFailure();
    }

    public override VideoBackend CreateBackend(VideoInstance instance) => new MediaEngineVideoBackend(instance, this);
}

internal static class MediaEngineVideoBackendModule
{
    [ModuleInitializer]
    public static void Initialize() => VideoBackendRegistry.Register(new MediaEngineVideoBackendFactory());
}

#endif
