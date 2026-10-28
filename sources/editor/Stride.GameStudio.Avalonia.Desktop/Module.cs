// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.GameStudio.Avalonia.Desktop;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Override import resolution for Avalonia native dependencies: Stride's helper finds them next to the editor
        // (its registered dependency directories) and loads each one once
        OverrideImportResolution<HarfBuzzSharp.ContentType>();
        OverrideImportResolution<SkiaSharp.GRBackend>();
        // ANGLE, Avalonia's GPU renderer on Windows: Avalonia imports av_libGLESv2.dll but its package ships
        // av_libglesv2.dll, which a case-sensitive folder doesn't find, and then Avalonia renders the whole editor in
        // software. Only that library goes through the helper (lowercase): Avalonia.Win32's other imports are system libraries
        OverrideImportResolution<global::Avalonia.Win32PlatformOptions>(name => name.StartsWith("av_lib", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(name).ToLowerInvariant() : null);
        return;

        static void OverrideImportResolution<T>(Func<string, string?>? map = null)
        {
            NativeLibrary.SetDllImportResolver(typeof(T).Assembly, (name, _, _) =>
            {
                var libraryName = map != null ? map(name) : name;
                if (libraryName == null)
                    return IntPtr.Zero;
                try
                {
                    return NativeLibraryHelper.PreloadLibrary(libraryName, typeof(T));
                }
                catch (Exception)
                {
                    // Not known here: the runtime's own probing takes over
                    return IntPtr.Zero;
                }
            });
        }
    }
}
