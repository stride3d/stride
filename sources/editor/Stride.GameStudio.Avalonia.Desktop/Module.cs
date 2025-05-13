using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.GameStudio.Avalonia.Desktop;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Override import resolution for Avalonia native dependencies
        NativeLibrary.SetDllImportResolver(typeof(HarfBuzzSharp.ContentType).Assembly, (name, _, _) => NativeLibraryHelper.PreloadLibrary(name, typeof(HarfBuzzSharp.ContentType)));
        NativeLibrary.SetDllImportResolver(typeof(SkiaSharp.GRBackend).Assembly, (name, _, _) => NativeLibraryHelper.PreloadLibrary(name, typeof(SkiaSharp.GRBackend)));
    }
}
