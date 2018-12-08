// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.VirtualReality
{
    internal static class NativeInvoke
    {
#if XENKO_PLATFORM_IOS
        internal const string Library = "__Internal";
#else
        internal const string Library = "libxenkovr";
#endif

        internal static void PreLoad()
        {
#if XENKO_PLATFORM_WINDOWS
            NativeLibrary.PreloadLibrary(Library + ".dll", typeof(NativeInvoke));
#else
            NativeLibrary.PreloadLibrary(Library + ".so", typeof(NativeInvoke));
#endif
        }

        static NativeInvoke()
        {
            PreLoad();
        }
    }
}
