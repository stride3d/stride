// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

            initialized = true;
            // av_register_all / avcodec_register_all removed in FFmpeg 4.0 / 5.0 — registration is automatic now.
            ffmpeg.avformat_network_init();
        }

        /// <summary>
        /// Preload all FFmpeg libraries.
        /// </summary>
        /// <remarks>
        /// Must be called before any attempt to use FFmpeg API or this will have no effect.
        /// </remarks>
        public static void PreloadLibraries()
        {
            if (!CheckPlatformSupport() || initialized)
                return;

            // Note: order matters (dependencies)
            // avdevice
            //   |---- avfilter
            //   |       |---- avformat
            //   |       |       |---- avcodec
            //   |       |       |       |---- swresample
            //   |       |       |       |       |---- avutil
            //   |       |       |       |---- avutil
            //   |       |       |---- avutil
            //   |       |---- swscale
            //   |       |       |---- avutil
            //   |       |---- swresample
            //   |       |---- avutil
            //   |---- avformat
            //   |---- avcodec
            //   |---- avutil
            // FFmpeg.AutoGen 7.x resolves libav* through its own LibraryLoader which probes
            // ffmpeg.RootPath. Default ("") only searches the OS standard paths and never finds
            // SONAME-versioned libavutil.so.59 / libavutil.59.dylib we ship.
            // Two layouts to handle: tests flatten libs into the assembly dir, regular asm builds
            // keep them in runtimes/<rid>/native/. Check both.
            var asmDir = Path.GetDirectoryName(typeof(FFmpegUtils).Assembly.Location);
            if (asmDir != null)
            {
                var rid = GetCurrentRid();
                var runtimesDir = rid != null ? Path.Combine(asmDir, "runtimes", rid, "native") : null;
                // RID builds flatten DLLs to asmDir; portable builds keep them under runtimes/<rid>/native/.
                var probe = Platform.Type switch
                {
                    PlatformType.Windows => "avutil-59.dll",
                    PlatformType.macOS => "libavutil.59.dylib",
                    _ => "libavutil.so.59",
                };
                ffmpeg.RootPath = runtimesDir != null && File.Exists(Path.Combine(runtimesDir, probe)) ? runtimesDir : asmDir;
            }

            if (Platform.Type == PlatformType.Windows)
            {
                var type = typeof(FFmpegUtils);
                NativeLibraryHelper.PreloadLibrary("avutil-59", type);
                NativeLibraryHelper.PreloadLibrary("swresample-5", type);
                NativeLibraryHelper.PreloadLibrary("avcodec-61", type);
                NativeLibraryHelper.PreloadLibrary("avformat-61", type);
                NativeLibraryHelper.PreloadLibrary("swscale-8", type);
                NativeLibraryHelper.PreloadLibrary("avfilter-10", type);
                NativeLibraryHelper.PreloadLibrary("avdevice-61", type);
            }
            else
            {
                // Force-load via a no-op API call so dlopen happens here (clearer stacks on failure).
                uint version;
                version = ffmpeg.avutil_version();
                version = ffmpeg.swresample_version();
                version = ffmpeg.avcodec_version();
                version = ffmpeg.avformat_version();
                version = ffmpeg.swscale_version();
                version = ffmpeg.avfilter_version();
                version = ffmpeg.avdevice_version();
            }
        }

        private static string GetCurrentRid()
        {
            string os;
            if (OperatingSystem.IsWindows()) os = "win";
            else if (OperatingSystem.IsLinux()) os = "linux";
            else if (OperatingSystem.IsMacOS()) os = "osx";
            else return null;

            var arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => null,
            };
            return arch == null ? null : $"{os}-{arch}";
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
