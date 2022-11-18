// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
#if STRIDE_PLATFORM_IOS
using ObjCRuntime;
#endif

namespace Stride.Core.Native
{
    public static class NativeInvoke
    {
#if STRIDE_PLATFORM_IOS
        internal const string Library = "__Internal";
#else
        internal const string Library = "libcore";
#endif

        static NativeInvoke()
        {
            NativeLibraryHelper.PreloadLibrary("libcore", typeof(NativeInvoke));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ManagedLogDelegate(string log);

        private static ManagedLogDelegate managedLogDelegateSingleton;

#if STRIDE_PLATFORM_IOS
        [MonoPInvokeCallback(typeof(ManagedLogDelegate))]
#endif
        private static void ManagedLog(string log)
        {
            Debug.WriteLine(log);
        }

        public static void Setup()
        {
            managedLogDelegateSingleton = ManagedLog;

#if !STRIDE_PLATFORM_IOS
            var ptr = Marshal.GetFunctionPointerForDelegate(managedLogDelegateSingleton);
#else
            var ptr = managedLogDelegateSingleton;
#endif

            CoreNativeSetup(ptr);
        }

#if !STRIDE_PLATFORM_IOS
        [SuppressUnmanagedCodeSecurity]
        [DllImport(Library, EntryPoint = "cnSetup", CallingConvention = CallingConvention.Cdecl)]
        private static extern void CoreNativeSetup(IntPtr logger);
#else
        [SuppressUnmanagedCodeSecurity]
        [DllImport(Library, EntryPoint = "cnSetup", CallingConvention = CallingConvention.Cdecl)]
        private static extern void CoreNativeSetup(ManagedLogDelegate logger);
#endif
    }
}
