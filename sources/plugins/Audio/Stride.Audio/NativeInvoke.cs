// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Audio
{
    internal static class NativeInvoke
    {
#if STRIDE_PLATFORM_IOS
        internal const string Library = "__Internal";
#else
        internal const string Library = "libstrideaudio";
#endif

        internal static void PreLoad()
        {
            NativeLibraryHelper.PreloadLibrary("libstrideaudio", typeof(NativeInvoke));
        }

        static NativeInvoke()
        {
            PreLoad();
        }
    }
}
