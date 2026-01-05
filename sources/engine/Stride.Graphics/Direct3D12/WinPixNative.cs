// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UnmanagedType = System.Runtime.InteropServices.UnmanagedType;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

using Stride.Core;
using Stride.Core.Mathematics;

using static Stride.Graphics.Win32;

namespace Stride.Graphics;

/// <summary>
///   Provides methods and helpers for integrating with Microsoft PIX profiling tools, including support
///   for GPU capture and event instrumentation in Direct3D 12 applications.
/// </summary>
public static class WinPixNative
{
    private const string RuntimeDllName = "WinPixEventRuntime.dll";
    private const string CapturerDllName = "WinPixGpuCapturer.dll";

    internal static void PreLoad()
    {
        NativeLibraryHelper.PreloadLibrary("WinPixEventRuntime", typeof(WinPixNative));
    }

    static WinPixNative()
    {
        PreLoad();
    }

    /// <summary>
    ///   Ensures that the <c>WinPixGpuCapturer</c> DLL is loaded into the current process,
    ///   enabling GPU capture functionality for PIX profiling tools.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   Could not locate the Microsoft PIX installation folder.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    ///   The Microsoft PIX installation directory does not exist under the "Program Files" folder.
    /// </exception>
    /// <remarks>
    ///   This method is typically called before initializing GPU capture or profiling with Microsoft PIX.
    ///   If the DLL is already loaded, calling this method has no effect.
    /// </remarks>
    public static void LoadPixGpuCapturer()
    {
        IntPtr moduleHandle = GetModuleHandle(CapturerDllName);
        if (moduleHandle == IntPtr.Zero)
        {
            LoadLibrary(GetLatestWinPixGpuCapturerPath());
        }

        //
        // Helper to locate the latest installed "WinPixGpuCapturer.dll".
        //
        static string GetLatestWinPixGpuCapturerPath()
        {
            string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (string.IsNullOrEmpty(programFilesPath))
                throw new DirectoryNotFoundException("Could not locate \"Program Files\" folder.");

            string pixInstallationPath = Path.Combine(programFilesPath, "Microsoft PIX");
            if (!Directory.Exists(pixInstallationPath))
                throw new DirectoryNotFoundException($"Microsoft PIX not found under \"{programFilesPath}\".");

            string newestVersionFound = null;

            // Enumerate all subdirectories, looking for the one with the highest version number
            foreach (var dir in Directory.EnumerateDirectories(pixInstallationPath))
            {
                var dirName = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(dirName))
                    continue;

                if (newestVersionFound is null || string.CompareOrdinal(newestVersionFound, dirName) < 0)
                {
                    newestVersionFound = dirName;
                }
            }

            if (string.IsNullOrEmpty(newestVersionFound))
                throw new InvalidOperationException("No PIX installation found.");

            // Return full path to WinPixGpuCapturer.dll
            string dllPath = Path.Combine(pixInstallationPath, newestVersionFound, CapturerDllName);
            return dllPath;
        }
    }


    /// <summary>
    ///   Ends the current PIX event on the specified Direct3D 12 Command List.
    /// </summary>
    /// <param name="commandList">
    ///   The Direct3D 12 Command List on which to end the PIX event. Must not be <see langword="null"/>.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void PIXEndEventOnCommandList(ComPtr<ID3D12GraphicsCommandList> commandList)
    {
        PIXEndEventOnCommandList((nint) commandList.Handle);
    }

    public enum PIXCaptureParametersType : uint
    {
        PIX_CAPTURE_GPU = 1 << 1,
    }

    // Define the managed equivalent of the PIXCaptureParameters structure
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PIXCaptureParametersGPU
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string FileName;
    }

    // void WINAPI PIXBeginCapture2
    [DllImport(RuntimeDllName, EntryPoint = "PIXBeginCapture2",
        CallingConvention = CallingConvention.StdCall)]
    internal static extern void PIXBeginCapture2(PIXCaptureParametersType type, ref PIXCaptureParametersGPU parameters);

    // void WINAPI PIXBeginCapture2
    [DllImport(RuntimeDllName, EntryPoint = "PIXEndCapture",
        CallingConvention = CallingConvention.StdCall)]
    internal static extern void PIXEndCapture(bool discard);

    // void WINAPI PIXEndEventOnCommandList(ID3D12GraphicsCommandList* commandList)
    [DllImport(RuntimeDllName, EntryPoint = "PIXEndEventOnCommandList", CallingConvention = CallingConvention.StdCall)]
    private static extern void PIXEndEventOnCommandList(IntPtr commandList);

    /// <summary>
    ///   Ends the current PIX event on the specified Direct3D 12 Command Queue.
    /// </summary>
    /// <param name="commandQueue">
    ///   The Direct3D 12 Command Queue on which to end the PIX event. Must not be <see langword="null"/>.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void PIXEndEventOnCommandQueue(ComPtr<ID3D12CommandQueue> commandQueue)
    {
        PIXEndEventOnCommandQueue((nint) commandQueue.Handle);
    }

    // void WINAPI PIXEndEventOnCommandQueue(ID3D12CommandQueue* commandQueue)
    [DllImport(RuntimeDllName, EntryPoint = "PIXEndEventOnCommandQueue", CallingConvention = CallingConvention.StdCall)]
    private static extern void PIXEndEventOnCommandQueue(IntPtr commandQueue);

    /// <summary>
    ///   Begins a PIX event on the specified Direct3D 12 Command List with the given color and event name.
    /// </summary>
    /// <param name="commandList">
    ///   The Direct3D 12 Command List on which to begin the PIX event. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="profileColor">The color to associate with the PIX event.</param>
    /// <param name="name">
    ///   The name of the event to display in PIX. Can be <see langword="null"/> or empty if no name is desired.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void PIXBeginEventOnCommandList(ComPtr<ID3D12GraphicsCommandList> commandList, Color4 profileColor, string? name)
    {
        PIXBeginEventOnCommandList((nint) commandList.Handle, (uint) profileColor.ToBgra(), name);
    }

    // void WINAPI PIXBeginEventOnCommandList(ID3D12GraphicsCommandList* commandList, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXBeginEventOnCommandList", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private static extern void PIXBeginEventOnCommandList(
        IntPtr commandList,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);

    /// <summary>
    ///   Begins a PIX event on the specified Direct3D 12 Command Queue with the given color and event name.
    /// </summary>
    /// <param name="commandQueue">
    ///   The Direct3D 12 Command Queue on which to begin the PIX event. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="profileColor">The color to associate with the PIX event.</param>
    /// <param name="name">
    ///   The name of the event to display in PIX. Can be <see langword="null"/> or empty if no name is desired.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void PIXBeginEventOnCommandQueue(ComPtr<ID3D12CommandQueue> commandQueue, Color4 profileColor, string? name)
    {
        PIXBeginEventOnCommandQueue((nint) commandQueue.Handle, (uint) profileColor.ToBgra(), name);
    }

    // void WINAPI PIXBeginEventOnCommandQueue(ID3D12CommandQueue* commandQueue, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXBeginEventOnCommandQueue", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private static extern void PIXBeginEventOnCommandQueue(
        IntPtr commandQueue,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);

    /// <summary>
    ///   Sets a PIX marker on the specified Direct3D 12 Command Queue with the given color and event name.
    /// </summary>
    /// <param name="commandList">
    ///   The Direct3D 12 Command Queue on which to set the PIX marker. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="profileColor">The color to associate with the PIX marker.</param>
    /// <param name="name">
    ///   The name of the marker to display in PIX. Can be <see langword="null"/> or empty if no name is desired.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void PIXSetMarkerOnCommandList(ComPtr<ID3D12GraphicsCommandList> commandList, Color4 profileColor, string? name)
    {
        PIXSetMarkerOnCommandList((nint) commandList.Handle, (uint) profileColor.ToBgra(), name);
    }

    // void WINAPI PIXSetMarkerOnCommandList(ID3D12GraphicsCommandList* commandList, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXSetMarkerOnCommandList", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private static extern void PIXSetMarkerOnCommandList(
        IntPtr commandList,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);

    /// <summary>
    ///   Sets a PIX marker on the specified Direct3D 12 Command Queue with the given color and event name.
    /// </summary>
    /// <param name="commandQueue">
    ///   The Direct3D 12 Command Queue on which to set the PIX marker. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="profileColor">The color to associate with the PIX marker.</param>
    /// <param name="name">
    ///   The name of the marker to display in PIX. Can be <see langword="null"/> or empty if no name is desired.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void PIXSetMarkerOnCommandQueue(ComPtr<ID3D12CommandQueue> commandQueue, Color4 profileColor, string? name)
    {
        PIXSetMarkerOnCommandQueue((nint) commandQueue.Handle, (uint) profileColor.ToBgra(), name);
    }

    // void WINAPI PIXSetMarkerOnCommandQueue(ID3D12CommandQueue* commandQueue, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXSetMarkerOnCommandQueue", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private static extern void PIXSetMarkerOnCommandQueue(
        IntPtr commandQueue,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);
}

#endif
