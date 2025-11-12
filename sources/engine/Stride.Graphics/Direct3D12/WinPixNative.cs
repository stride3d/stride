// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core;

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

    public static void LoadPixGpuCapturer()
    {
        IntPtr moduleHandle = GetModuleHandle(CapturerDllName);
        if (moduleHandle == IntPtr.Zero)
        {
            LoadLibrary(GetLatestWinPixGpuCapturerPath());
        }
    }

    private static string GetLatestWinPixGpuCapturerPath()
    {
        // Equivalent to SHGetKnownFolderPath(FOLDERID_ProgramFiles, ...)
        string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (string.IsNullOrEmpty(programFilesPath))
            throw new InvalidOperationException("Could not locate Program Files folder.");

        // Construct PIX installation path
        string pixInstallationPath = Path.Combine(programFilesPath, "Microsoft PIX");
        if (!Directory.Exists(pixInstallationPath))
            throw new DirectoryNotFoundException($"Microsoft PIX not found under {programFilesPath}");

        string newestVersionFound = null;

        // Enumerate all subdirectories (like std::filesystem::directory_iterator)
        foreach (var dir in Directory.EnumerateDirectories(pixInstallationPath))
        {
            var dirName = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(dirName))
                continue;

            // Equivalent to (newestVersionFound.empty() || newestVersionFound < dirName)
            if (newestVersionFound == null || string.CompareOrdinal(newestVersionFound, dirName) < 0)
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

    // void WINAPI PIXEndEventOnCommandList(ID3D12GraphicsCommandList* commandList)
    [DllImport(RuntimeDllName, EntryPoint = "PIXEndEventOnCommandList",
        CallingConvention = CallingConvention.StdCall)]
    internal static extern void PIXEndEventOnCommandList(IntPtr commandList);

    // void WINAPI PIXEndEventOnCommandQueue(ID3D12CommandQueue* commandQueue)
    [DllImport(RuntimeDllName, EntryPoint = "PIXEndEventOnCommandQueue",
        CallingConvention = CallingConvention.StdCall)]
    internal static extern void PIXEndEventOnCommandQueue(IntPtr commandQueue);

    // void WINAPI PIXBeginEventOnCommandList(ID3D12GraphicsCommandList* commandList, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXBeginEventOnCommandList",
        CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void PIXBeginEventOnCommandList(
        IntPtr commandList,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);

    // void WINAPI PIXBeginEventOnCommandQueue(ID3D12CommandQueue* commandQueue, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXBeginEventOnCommandQueue",
        CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void PIXBeginEventOnCommandQueue(
        IntPtr commandQueue,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);

    // void WINAPI PIXSetMarkerOnCommandList(ID3D12GraphicsCommandList* commandList, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXSetMarkerOnCommandList",
        CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void PIXSetMarkerOnCommandList(
        IntPtr commandList,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);

    // void WINAPI PIXSetMarkerOnCommandQueue(ID3D12CommandQueue* commandQueue, UINT64 color, PCSTR formatString)
    [DllImport(RuntimeDllName, EntryPoint = "PIXSetMarkerOnCommandQueue",
        CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void PIXSetMarkerOnCommandQueue(
        IntPtr commandQueue,
        ulong color,
        [MarshalAs(UnmanagedType.LPStr)] string formatString);

    [DllImport("kernel32", EntryPoint = "LoadLibrary", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}

#endif
