// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Graphics;

/// <summary>
///   Provides P/Invoke declarations for some Win32 kernel functions.
/// </summary>
internal static class Win32
{
    [DllImport("kernel32", EntryPoint = "LoadLibrary", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAY_DEVICEW
    {
        public uint cb;

        public WChar32 DeviceName;
        public WChar128 DeviceString;

        public uint StateFlags;

        public WChar128 DeviceID;
        public WChar128 DeviceKey;
    }

    // BOOL EnumDisplayDevicesW(LPCWSTR lpDevice, DWORD iDevNum, PDISPLAY_DEVICEW lpDisplayDevice, DWORD dwFlags);
    [DllImport("user32", ExactSpelling = true)]
    public static unsafe extern BOOL EnumDisplayDevicesW(char* lpDevice, uint iDevNum, DISPLAY_DEVICEW* lpDisplayDevice, uint dwFlags);

    #region Helper structs and types

    public readonly struct BOOL(int value)
    {
        public readonly int Value = value;

        public static BOOL FALSE => new(0);
        public static BOOL TRUE => new(1);

        public static bool operator ==(BOOL left, BOOL right) => left.Value == right.Value;

        public static bool operator !=(BOOL left, BOOL right) => left.Value != right.Value;


        public static implicit operator bool(BOOL value) => value.Value != 0;
        public static implicit operator BOOL(bool value) => new(value ? 1 : 0);

        public static bool operator false(BOOL value) => value.Value == 0;
        public static bool operator true(BOOL value) => value.Value != 0;

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();
    }

    // WCHAR[32]
    [InlineArray(Length)]
    public struct WChar32
    {
        public const int Length = 32;
        public char e0;

        public Span<char> AsSpan() => MemoryMarshal.CreateSpan(ref e0, Length);
    }

    // WCHAR[128]
    [InlineArray(Length)]
    public struct WChar128
    {
        public const int Length = 128;
        public char e0;

        public Span<char> AsSpan() => MemoryMarshal.CreateSpan(ref e0, Length);
    }

    #endregion
}

#endif
