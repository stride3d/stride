// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.IO;
using System.Runtime.InteropServices;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

using Stride.Core.Diagnostics;

namespace Stride.Graphics;

/// <summary>
///   Creates the Direct3D 12 devices and debug interfaces, with the Agility SDK shipped under <c>D3D12\</c> when there is one.
/// </summary>
/// <remarks>
///   The Agility SDK is loaded through an <see cref="ID3D12DeviceFactory"/>, which needs a Windows update from September 2023
///   or newer. Its devices are independent: every <see cref="CreateDevice"/> creates a new native device.
/// </remarks>
internal static unsafe class D3D12Runtime
{
    private static readonly Logger Log = GlobalLogger.GetLogger(nameof(GraphicsDevice));

    public static readonly Guid CLSID_D3D12Debug = new(0xf2352aeb, 0xdd84, 0x49fe, 0xb9, 0x7b, 0xa9, 0xdc, 0xfd, 0xcc, 0x1b, 0x4f);
    public static readonly Guid CLSID_D3D12DeviceRemovedExtendedData = new(0x4a75bbc4, 0x9ff4, 0x4ad8, 0x9f, 0x18, 0xab, 0xae, 0x84, 0xdc, 0x5f, 0xf2);
    private static readonly Guid CLSID_D3D12SDKConfiguration = new(0x7cda6aca, 0xa03e, 0x49c8, 0x94, 0x58, 0x03, 0x34, 0xd2, 0x0e, 0x07, 0xce);

    private static readonly D3D12 d3d12 = D3D12.GetApi();

    private static readonly object initializationLock = new();
    private static bool initialized;
    private static ID3D12DeviceFactory* deviceFactory;

    /// <summary>
    ///   Gets a value indicating whether the devices are created with the Agility SDK shipped under <c>D3D12\</c>.
    /// </summary>
    public static bool IsAgilitySDKLoaded => GetDeviceFactory() is not null;

    /// <summary>
    ///   Creates a native device on <paramref name="adapter"/> (or the default adapter when it is null).
    /// </summary>
    public static HResult CreateDevice(ComPtr<IUnknown> adapter, D3DFeatureLevel minimumFeatureLevel, out ComPtr<ID3D12Device> device)
    {
        var factory = GetDeviceFactory();
        return factory is not null
            ? factory->CreateDevice(adapter, minimumFeatureLevel, out device)
            : d3d12.CreateDevice(adapter, minimumFeatureLevel, out device);
    }

    /// <summary>
    ///   Checks that <paramref name="adapter"/> supports <paramref name="featureLevel"/>, without creating a device.
    /// </summary>
    public static bool IsFeatureLevelSupported(IUnknown* adapter, D3DFeatureLevel featureLevel)
    {
        var deviceGuid = ID3D12Device.Guid;
        var factory = GetDeviceFactory();
        HResult result = factory is not null
            ? factory->CreateDevice(adapter, featureLevel, &deviceGuid, (void**) null)
            : d3d12.CreateDevice(adapter, featureLevel, &deviceGuid, (void**) null);
        return result.IsSuccess;
    }

    /// <summary>
    ///   Gets a debug interface (<see cref="CLSID_D3D12Debug"/> or <see cref="CLSID_D3D12DeviceRemovedExtendedData"/>)
    ///   that applies to the devices created after it is configured.
    /// </summary>
    public static HResult GetDebugInterface<T>(Guid clsid, out ComPtr<T> debugInterface) where T : unmanaged, IComVtbl<T>
    {
        var factory = GetDeviceFactory();
        return factory is not null
            ? factory->GetConfigurationInterface(ref clsid, out debugInterface)
            : d3d12.GetDebugInterface(out debugInterface);
    }

    private static ID3D12DeviceFactory* GetDeviceFactory()
    {
        lock (initializationLock)
        {
            if (!initialized)
            {
                initialized = true;
                deviceFactory = CreateDeviceFactory();
            }
            return deviceFactory;
        }
    }

    private static ID3D12DeviceFactory* CreateDeviceFactory()
    {
        var sdkPath = Path.Combine(AppContext.BaseDirectory, "D3D12") + Path.DirectorySeparatorChar;
        var corePath = Path.Combine(sdkPath, "D3D12Core.dll");
        if (!File.Exists(corePath))
        {
            Log.Info("[D3D12] Using the system D3D12 runtime.");
            return null;
        }

        // The factory accepts only the exact version of the DLL
        if (!TryReadSDKVersion(corePath, out var sdkVersion))
        {
            Log.Warning($"[D3D12] {corePath} has no D3D12SDKVersion export; using the system D3D12 runtime.");
            return null;
        }

        var clsid = CLSID_D3D12SDKConfiguration;
        HResult result = d3d12.GetInterface(ref clsid, out ComPtr<ID3D12SDKConfiguration1> sdkConfiguration);
        if (result.IsFailure)
        {
            Log.Warning($"[D3D12] This Windows version can't load the Agility SDK {sdkVersion} (0x{result.Value:X8}, a Windows update from September 2023 or newer is needed); using the system D3D12 runtime.");
            return null;
        }

        result = sdkConfiguration.CreateDeviceFactory(sdkVersion, sdkPath, out ComPtr<ID3D12DeviceFactory> factory);
        sdkConfiguration.Dispose();
        if (result.IsFailure)
        {
            Log.Warning($"[D3D12] Can't load the Agility SDK {sdkVersion} from {sdkPath} (0x{result.Value:X8}); using the system D3D12 runtime.");
            return null;
        }

        Log.Info($"[D3D12] Using the Agility SDK {sdkVersion} from {sdkPath}.");
        return factory.Handle;
    }

    private static bool TryReadSDKVersion(string corePath, out uint sdkVersion)
    {
        sdkVersion = 0;
        if (!NativeLibrary.TryLoad(corePath, out var module))
            return false;

        try
        {
            if (!NativeLibrary.TryGetExport(module, "D3D12SDKVersion", out var address))
                return false;

            sdkVersion = *(uint*) address;
            return true;
        }
        finally
        {
            NativeLibrary.Free(module);
        }
    }
}

#endif
