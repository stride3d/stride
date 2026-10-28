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
        return;

        static void OverrideImportResolution<T>()
        {
            NativeLibrary.SetDllImportResolver(typeof(T).Assembly, (name, _, _) =>
            {
                try
                {
                    return NativeLibraryHelper.PreloadLibrary(name, typeof(T));
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
