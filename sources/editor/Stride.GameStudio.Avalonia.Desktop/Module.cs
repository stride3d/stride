using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.GameStudio.Avalonia.Desktop;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Override import resolution for Avalonia native dependencies
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
                    return nint.Zero;
                }
            });
        }
    }
}
