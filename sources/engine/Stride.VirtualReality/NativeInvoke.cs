// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.VirtualReality
{
    internal static class NativeInvoke
    {
#if STRIDE_PLATFORM_IOS
        internal const string Library = "__Internal";
#else
        internal const string Library = "libstridevr";
#endif

        internal static void PreLoad()
        {
            NativeLibraryHelper.PreloadLibrary("libstridevr", typeof(NativeInvoke));
        }

        static NativeInvoke()
        {
            PreLoad();
        }
    }
}
