// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Rectangle = Stride.Core.Mathematics.Rectangle;

namespace Stride.Graphics;

/// <summary>
/// Enumerates Windows outputs through DXGI and reads active display modes from Windows APIs.
/// </summary>
internal static unsafe class WindowsDisplayProvider
{
    internal static DisplayInfo[] Enumerate()
    {
        var outputs = new List<DisplayInfo>();
        var refreshRates = ReadRefreshRates();
        using var dxgi = DXGI.GetApi();
        if (((HResult)dxgi.CreateDXGIFactory1<IDXGIFactory1>(out var factory)).IsFailure)
            return [];
        try
        {
            for (uint index = 0; ; index++)
            {
                ComPtr<IDXGIAdapter1> adapter = default;
                if (((HResult)factory.EnumAdapters1(index, ref adapter)).IsFailure)
                    break;
                try
                {
                    AdapterDesc1 adapterDesc;
                    if (((HResult)adapter.GetDesc1(&adapterDesc)).IsFailure)
                        continue;
                    long luid = Unsafe.ReadUnaligned<long>(&adapterDesc.AdapterLuid);
                    for (uint outputIndex = 0; ; outputIndex++)
                    {
                        ComPtr<IDXGIOutput> output = default;
                        if (((HResult)adapter.EnumOutputs(outputIndex, ref output)).IsFailure)
                            break;
                        try
                        {
                            OutputDesc desc;
                            if (((HResult)output.GetDesc(&desc)).IsFailure || !desc.AttachedToDesktop)
                                continue;
                            var rect = desc.DesktopCoordinates;
                            var bounds = new Rectangle(rect.Min.X, rect.Min.Y, rect.Max.X - rect.Min.X, rect.Max.Y - rect.Min.Y);
                            if (bounds.Width <= 0 || bounds.Height <= 0)
                                continue;
                            var name = new string(desc.DeviceName);
                            var modes = ReadModes(output);
                            var current = ReadCurrentMode(name, refreshRates);
                            outputs.Add(new DisplayInfo(name, bounds, current, modes, luid, desc.Monitor));
                        }
                        finally { output.Dispose(); }
                    }
                }
                finally { adapter.Dispose(); }
            }
        }
        finally { factory.Dispose(); }
        return outputs.ToArray();
    }

    private static DisplayMode[] ReadModes(ComPtr<IDXGIOutput> output)
    {
        var result = new List<DisplayMode>();
        var seen = new HashSet<DisplayMode>();
        // These are displayable color formats, not every DXGI resource format.
        ReadFormat(Format.FormatR8G8B8A8Unorm);
        ReadFormat(Format.FormatB8G8R8A8Unorm);
        ReadFormat(Format.FormatR10G10B10A2Unorm);
        ReadFormat(Format.FormatR16G16B16A16Float);
        return result.ToArray();

        void ReadFormat(Format format)
        {
            // A monitor can be unplugged between the count and fill calls.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                uint count = 0;
                if (((HResult)output.GetDisplayModeList(format, 0, &count, (ModeDesc*)null)).IsFailure || count == 0)
                    return;
                var modes = new ModeDesc[count];
                fixed (ModeDesc* pointer = modes)
                {
                    var hr = (HResult)output.GetDisplayModeList(format, 0, &count, pointer);
                    if (hr.IsFailure)
                        continue;
                }
                for (int i = 0; i < count; i++)
                {
                    var mode = modes[i];
                    if (mode.Width == 0 || mode.Height == 0)
                        continue;
                    var converted = new DisplayMode((PixelFormat)mode.Format, (int)mode.Width, (int)mode.Height,
                        new Rational((int)mode.RefreshRate.Numerator,
                            mode.RefreshRate.Denominator > 0 ? (int)mode.RefreshRate.Denominator : 1));
                    if (seen.Add(converted))
                        result.Add(converted);
                }
                return;
            }
        }
    }

    private static DisplayMode? ReadCurrentMode(string deviceName, Dictionary<string, Rational> refreshRates)
    {
        DeviceMode native = default;
        native.Size = (ushort)sizeof(DeviceMode);
        if (!EnumDisplaySettingsW(deviceName, -1, &native) || native.Width == 0 || native.Height == 0)
            return null;

        // EnumDisplaySettings supplies actual pixels, not DPI-virtualized bounds.
        // QueryDisplayConfig supplies the active rational timing where available.
        var format = native.BitsPerPixel == 16 ? PixelFormat.B5G6R5_UNorm : PixelFormat.B8G8R8A8_UNorm;
        var refresh = new Rational(native.Frequency > 1 ? (int)native.Frequency : 0, 1);
        if (refreshRates.TryGetValue(deviceName, out var precise))
            refresh = precise;
        return new DisplayMode(format, (int)native.Width, (int)native.Height, refresh);
    }

    private static Dictionary<string, Rational> ReadRefreshRates()
    {
        var result = new Dictionary<string, Rational>(StringComparer.OrdinalIgnoreCase);
        const uint OnlyActivePaths = 2;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            uint pathCount, modeCount;
            if (GetDisplayConfigBufferSizes(OnlyActivePaths, &pathCount, &modeCount) != 0)
                break;
            var paths = new DisplayPath[pathCount];
            var modes = new DisplayConfigMode[modeCount];
            fixed (DisplayPath* pathPointer = paths)
            fixed (DisplayConfigMode* modePointer = modes)
            {
                int error = QueryDisplayConfig(OnlyActivePaths, &pathCount, pathPointer, &modeCount, modePointer, null);
                if (error == 122) // ERROR_INSUFFICIENT_BUFFER: topology changed.
                    continue;
                if (error != 0)
                    break;
            }
            for (int index = 0; index < pathCount; index++)
            {
                var path = paths[index];
                SourceDeviceName source = default;
                source.Type = 1; // DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME
                source.Size = (uint)sizeof(SourceDeviceName);
                source.AdapterLuid = path.SourceLuid;
                source.Id = path.SourceId;
                if (DisplayConfigGetDeviceInfo(&source) == 0 && path.RefreshNumerator <= int.MaxValue &&
                    path.RefreshDenominator > 0 && path.RefreshDenominator <= int.MaxValue)
                {
                    result.TryAdd(new string(source.DeviceName),
                        new Rational((int)path.RefreshNumerator, (int)path.RefreshDenominator));
                }
            }
            break;
        }
        return result;
    }

    // Native DISPLAYCONFIG layouts. Only the fields needed for active timing
    // and GDI source identity are declared; the mode union is an opaque buffer.
    [StructLayout(LayoutKind.Explicit, Size = 72)]
    private struct DisplayPath
    {
        [FieldOffset(0)] public long SourceLuid;
        [FieldOffset(8)] public uint SourceId;
        [FieldOffset(48)] public uint RefreshNumerator;
        [FieldOffset(52)] public uint RefreshDenominator;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    private struct DisplayConfigMode { }

    [StructLayout(LayoutKind.Explicit, Size = 84, Pack = 4)]
    private struct SourceDeviceName
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(4)] public uint Size;
        [FieldOffset(8)] public long AdapterLuid;
        [FieldOffset(16)] public uint Id;
        [FieldOffset(20)] public fixed char DeviceName[32];
    }

    // DEVMODEW's display union/layout is fixed by the Windows ABI (220 bytes).
    [StructLayout(LayoutKind.Explicit, Size = 220)]
    private struct DeviceMode
    {
        [FieldOffset(68)] public ushort Size;
        [FieldOffset(168)] public uint BitsPerPixel;
        [FieldOffset(172)] public uint Width;
        [FieldOffset(176)] public uint Height;
        [FieldOffset(184)] public uint Frequency;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettingsW(string deviceName, int mode, DeviceMode* settings);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int GetDisplayConfigBufferSizes(uint flags, uint* pathCount, uint* modeCount);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int QueryDisplayConfig(uint flags, uint* pathCount, DisplayPath* paths,
        uint* modeCount, DisplayConfigMode* modes, uint* topology);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int DisplayConfigGetDeviceInfo(SourceDeviceName* source);
}
#endif
