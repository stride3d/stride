// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace Stride.CrashReport
{
    /// <summary>
    /// P/Invoke into <c>libstridecrash</c> (Native/StrideCrash.cpp), the native crash trigger. Windows-only; on
    /// other platforms the library is an inert stub and these are never called.
    /// </summary>
    /// <remarks>
    /// The library ships under <c>runtimes/&lt;rid&gt;/native/</c> (or flattened to the app directory for
    /// RID-specific publishes like GameStudio). This minimal assembly doesn't reference Stride.Core's
    /// NativeLibraryHelper, so it resolves the library from those locations itself.
    /// </remarks>
    internal static class NativeInvoke
    {
        internal const string Library = "libstridecrash";

        static NativeInvoke()
        {
            if (OperatingSystem.IsWindows())
                NativeLibrary.SetDllImportResolver(typeof(NativeInvoke).Assembly, Resolve);
        }

        /// <summary>Registers the native vectored exception handler. Paths must be absolute; the identity strings
        /// (application/version/environment) ride the reporter's command line; timeoutMs bounds the capture wait.</summary>
        [DllImport(Library, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern void stride_crash_install(string reporterPath, string dumpDir, uint timeoutMs,
            string application, string version, string environment);

        private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? paths)
        {
            if (name != Library)
                return IntPtr.Zero;

            var baseDir = AppContext.BaseDirectory;
            foreach (var candidate in new[]
            {
                Path.Combine(baseDir, "runtimes", "win-x64", "native", "libstridecrash.dll"),
                Path.Combine(baseDir, "libstridecrash.dll"),
            })
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
                    return handle;
            }
            return IntPtr.Zero; // fall back to the default search
        }
    }
}
