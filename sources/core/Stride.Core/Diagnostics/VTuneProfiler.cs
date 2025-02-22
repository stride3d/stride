// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP
using System.Runtime.InteropServices;

namespace Stride.Core.Diagnostics;

/// <summary>
/// This static class gives access to the Pause/Resume API of VTune Amplifier. It is available on Windows Desktop platform only.
/// </summary>
public static partial class VTuneProfiler
{
    private const string VTune2015DllName = "ittnotify_collector.dll";
    private static readonly Dictionary<string, StringHandle> StringHandles = [];

    /// <summary>
    /// Resumes the profiler.
    /// </summary>
    public static void Resume()
    {
        try
        {
            __itt_resume();
        }
        catch (DllNotFoundException)
        {
        }
    }

    /// <summary>
    /// Suspends the profiler.
    /// </summary>
    public static void Pause()
    {
        try
        {
            __itt_pause();
        }
        catch (DllNotFoundException)
        {
        }
    }

    public static readonly bool IsAvailable = NativeLibrary.Load(VTune2015DllName) != IntPtr.Zero;

    public static Event CreateEvent(string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        return IsAvailable ? __itt_event_createW(eventName, eventName.Length) : new Event();
    }

    public static Domain CreateDomain(string domaiName)
    {
        ArgumentNullException.ThrowIfNull(domaiName);
        return IsAvailable ? __itt_domain_createW(domaiName) : new Domain();
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Event
    {
        private readonly int id;

        public readonly void Start()
        {
            if (id == 0)
                return;
            _ = __itt_event_start(this);
        }

        public readonly void End()
        {
            if (id == 0)
                return;
            _ = __itt_event_end(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Domain
    {
        internal readonly IntPtr Pointer;

        public readonly void BeginFrame()
        {
            if (Pointer == IntPtr.Zero)
                return;
            __itt_frame_begin_v3(this, IntPtr.Zero);
        }

        public readonly void BeginTask(string taskName)
        {
            if (Pointer == IntPtr.Zero)
                return;
            __itt_task_begin(this, new IttId(), new IttId(), GetStringHandle(taskName));
        }

        public readonly void EndTask()
        {
            if (Pointer == IntPtr.Zero)
                return;
            __itt_task_end(this);
        }

        public readonly void EndFrame()
        {
            if (Pointer == IntPtr.Zero)
                return;
            __itt_frame_end_v3(this, IntPtr.Zero);
        }
    }

    private static StringHandle GetStringHandle(string text)
    {
        StringHandle result;
        lock (StringHandles)
        {
            if (!StringHandles.TryGetValue(text, out result))
            {
                result = __itt_string_handle_createW(text);
                StringHandles.Add(text, result);
            }
        }
        return result;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct StringHandle
    {
        private readonly IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private readonly struct IttId
    {
        private readonly long d1;
        private readonly long d2;
        private readonly long d3;
    }

#pragma warning disable SA1300 // Element must begin with upper-case letter
#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void __itt_resume();
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void __itt_resume();
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void __itt_pause();
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void __itt_pause();
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)] // not working
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void __itt_frame_begin_v3(Domain domain, IntPtr id);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)] // not working
    private static extern void __itt_frame_begin_v3(Domain domain, IntPtr id);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)] // not working
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void __itt_frame_end_v3(Domain domain, IntPtr id);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)] // not working
    private static extern void __itt_frame_end_v3(Domain domain, IntPtr id);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial Domain __itt_domain_createW([MarshalAs(UnmanagedType.LPWStr)] string domainName);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern Domain __itt_domain_createW([MarshalAs(UnmanagedType.LPWStr)] string domainName);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial Event __itt_event_createW([MarshalAs(UnmanagedType.LPWStr)] string eventName, int eventNameLength);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern Event __itt_event_createW([MarshalAs(UnmanagedType.LPWStr)] string eventName, int eventNameLength);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int __itt_event_start(Event eventHandler);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int __itt_event_start(Event eventHandler);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int __itt_event_end(Event eventHandler);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int __itt_event_end(Event eventHandler);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial StringHandle __itt_string_handle_createW([MarshalAs(UnmanagedType.LPWStr)] string text);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern StringHandle __itt_string_handle_createW([MarshalAs(UnmanagedType.LPWStr)] string text);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void __itt_task_begin(Domain domain, IttId taskid, IttId parentid, StringHandle name);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void __itt_task_begin(Domain domain, IttId taskid, IttId parentid, StringHandle name);
#endif // NET7_0_OR_GREATER

#if NET7_0_OR_GREATER
    [LibraryImport(VTune2015DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void __itt_task_end(Domain domain);
#else
    [DllImport(VTune2015DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void __itt_task_end(Domain domain);
#endif // NET7_0_OR_GREATER
#pragma warning restore SA1300 // Element must begin with upper-case letter
}

#endif
