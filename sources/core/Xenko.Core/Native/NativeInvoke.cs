// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
#if XENKO_PLATFORM_IOS
using ObjCRuntime;
#endif

namespace Xenko.Core.Native
{
    public static class NativeInvoke
    {
#if XENKO_PLATFORM_IOS
        internal const string Library = "__Internal";
        internal const string LibraryName = "libcore.so";
#else
        internal const string Library = "libcore";
#if XENKO_PLATFORM_WINDOWS
        internal const string LibraryName = "libcore.dll";
#else
        internal const string LibraryName = "libcore.so";
#endif
#endif

        static NativeInvoke()
        {
            NativeLibrary.PreloadLibrary(LibraryName, typeof(NativeInvoke));
        }

        /// <summary>
        /// Suspends current thread for <paramref name="ms"/> milliseconds.
        /// </summary>
        /// <param name="ms">Number of milliseconds to sleep.</param>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(Library, EntryPoint = "cnSleep", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Sleep(int ms);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ManagedLogDelegate(string log);

        private static ManagedLogDelegate managedLogDelegateSingleton;

#if XENKO_PLATFORM_IOS
        [MonoPInvokeCallback(typeof(ManagedLogDelegate))]
#endif
        private static void ManagedLog(string log)
        {
            Debug.WriteLine(log);
        }

        public static void Setup()
        {
            managedLogDelegateSingleton = ManagedLog;

#if !XENKO_PLATFORM_IOS
            var ptr = Marshal.GetFunctionPointerForDelegate(managedLogDelegateSingleton);
#else
            var ptr = managedLogDelegateSingleton;
#endif

            CoreNativeSetup(ptr);
        }

#if !XENKO_PLATFORM_IOS
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
