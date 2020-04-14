// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_VIDEO_FFMPEG
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Xenko.Core.Annotations;

namespace Xenko.Video.FFmpeg
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
#if (XENKO_PLATFORM_WINDOWS && !XENKO_RUNTIME_CORECLR) || XENKO_PLATFORM_ANDROID
            return true;
#else
            return false;
#endif
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
            ffmpeg.av_register_all();
            ffmpeg.avcodec_register_all();
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
#if XENKO_PLATFORM_WINDOWS
            var type = typeof(FFmpegUtils);
            Core.NativeLibrary.PreloadLibrary("avutil-55", type);
            Core.NativeLibrary.PreloadLibrary("swresample-2", type);
            Core.NativeLibrary.PreloadLibrary("avcodec-57", type);
            Core.NativeLibrary.PreloadLibrary("avformat-57", type);
            Core.NativeLibrary.PreloadLibrary("swscale-4", type);
            Core.NativeLibrary.PreloadLibrary("avfilter-6", type);
            Core.NativeLibrary.PreloadLibrary("avdevice-57", type);
#else
            uint version;
            version = ffmpeg.avutil_version();
            version = ffmpeg.swresample_version();
            version = ffmpeg.avcodec_version();
            version = ffmpeg.avformat_version();
            version = ffmpeg.swscale_version();
            version = ffmpeg.avfilter_version();
            version = ffmpeg.avdevice_version();
#endif
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
