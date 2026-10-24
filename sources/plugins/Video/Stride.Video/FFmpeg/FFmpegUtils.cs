// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Video.FFmpeg
{
    /// <summary>
    /// Collection of utilities when invoking <see cref="global::FFmpeg.AutoGen"/>.
    /// </summary>
    public static class FFmpegUtils
    {
        private static volatile bool initialized = false;
        private static volatile bool librariesPreloaded = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckPlatformSupport()
        {
            return Platform.Type == PlatformType.Windows
                || Platform.Type == PlatformType.Linux
                || Platform.Type == PlatformType.macOS
                || Platform.Type == PlatformType.Android;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsurePlatformSupport()
        {
            if (!CheckPlatformSupport())
                throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Initialize FFmpeg.
        /// </summary>
        public static void Initialize()
        {
            if (!CheckPlatformSupport() || initialized)
                return;

            // Ensure the native libraries (and our function resolver) are in place before the first
            // ffmpeg call triggers FFmpeg.AutoGen's one-shot binding.
            PreloadLibraries();

            initialized = true;
            // av_register_all / avcodec_register_all removed in FFmpeg 4.0 / 5.0 — registration is automatic now.
            ffmpeg.avformat_network_init();
        }

        // FFmpeg.AutoGen calls our resolver with these base names; map each to the version-stamped
        // Windows file name. Order matters: dependencies first (avcodec needs avutil, etc.).
        private static readonly (string Name, string WindowsName)[] Libraries =
        [
            ("avutil", "avutil-59"),
            ("swresample", "swresample-5"),
            ("avcodec", "avcodec-61"),
            ("avformat", "avformat-61"),
            ("swscale", "swscale-8"),
            ("avfilter", "avfilter-10"),
            ("avdevice", "avdevice-61"),
        ];

        private static readonly Dictionary<string, nint> libraryHandles = new();

        /// <summary>
        /// Preload all FFmpeg libraries.
        /// </summary>
        /// <remarks>
        /// Must be called before any attempt to use FFmpeg API or this will have no effect.
        /// </remarks>
        public static void PreloadLibraries()
        {
            if (!CheckPlatformSupport() || librariesPreloaded)
                return;
            librariesPreloaded = true;

            // Android ships the libs on the loader's default search path, so FFmpeg.AutoGen's own
            // resolver finds them; force-load via a no-op call so dlopen happens here (clearer stacks).
            if (Platform.Type == PlatformType.Android)
            {
                _ = ffmpeg.avutil_version();
                _ = ffmpeg.swresample_version();
                _ = ffmpeg.avcodec_version();
                _ = ffmpeg.avformat_version();
                _ = ffmpeg.swscale_version();
                _ = ffmpeg.avfilter_version();
                _ = ffmpeg.avdevice_version();
                return;
            }

            // Desktop: resolve FFmpeg's natives through NativeLibraryHelper (so libraries registered from
            // the NuGet cache are found) rather than its own RootPath probing. Must be set before any
            // ffmpeg member is touched: ffmpeg's static ctor binds every function once, keeping the first
            // non-null resolver.
            DynamicallyLoadedBindings.FunctionResolver = StrideFunctionResolver.Instance;

            // Load in dependency order so each library's imports bind to the already-loaded modules.
            // Windows files carry the version in the name (avutil-59.dll); Linux/macOS use the SONAME,
            // which NativeLibraryHelper matches from the base name.
            var type = typeof(FFmpegUtils);
            foreach (var library in Libraries)
            {
                var name = Platform.Type == PlatformType.Windows ? library.WindowsName : library.Name;
                libraryHandles[library.Name] = NativeLibraryHelper.PreloadLibrary(name, type);
            }
        }

        // Resolves FFmpeg.AutoGen function pointers from libraries loaded by NativeLibraryHelper, so
        // FFmpeg uses the same native resolution as the rest of the engine instead of ffmpeg.RootPath.
        private sealed class StrideFunctionResolver : IFunctionResolver
        {
            public static readonly StrideFunctionResolver Instance = new();

            public T GetFunctionDelegate<T>(string libraryName, string functionName, bool throwOnError)
            {
                if (!libraryHandles.TryGetValue(libraryName, out var handle) || handle == 0)
                {
                    if (throwOnError)
                        throw new DllNotFoundException($"FFmpeg library '{libraryName}' was not loaded.");
                    return default;
                }
                if (!NativeLibrary.TryGetExport(handle, functionName, out var function))
                {
                    if (throwOnError)
                        throw new EntryPointNotFoundException($"Could not find function '{functionName}' in FFmpeg library '{libraryName}'.");
                    return default;
                }
                return (T)(object)Marshal.GetDelegateForFunctionPointer(function, typeof(T));
            }
        }

        /// <summary>
        /// Converts a <see cref="AVDictionary"/>* to a Dictionary&lt;string,string&gt;.
        /// </summary>
        /// <param name="avDictionary">A pointer to a <see cref="AVDictionary"/> struct</param>
        /// <returns>A new dictionary containing a copy of all entries.</returns>
        [NotNull]
        internal static unsafe Dictionary<string, string> ToDictionary(AVDictionary* avDictionary)
        {
            var dictionary = new Dictionary<string, string>();
            if (avDictionary == null)
                return dictionary;

            AVDictionaryEntry* tag = null;
            while ((tag = ffmpeg.av_dict_get(avDictionary, string.Empty, tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
            {
                var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
                var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
                dictionary.Add(key, value);
            }
            return dictionary;
        }
    }
}
#endif
