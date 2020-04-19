// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Serialization.Contents
{
    /// <summary>
    /// Keys used for profiling the game class.
    /// </summary>
    public static class ContentProfilingKeys
    {
        public static readonly ProfilingKey Content = new ProfilingKey("Content");

        /// <summary>
        /// Profiling load of an asset.
        /// </summary>
        public static readonly ProfilingKey ContentLoad = new ProfilingKey(Content, "Load", ProfilingKeyFlags.Log);

        /// <summary>
        /// Profiling load of an asset.
        /// </summary>
        public static readonly ProfilingKey ContentReload = new ProfilingKey(Content, "Reload", ProfilingKeyFlags.Log);

        /// <summary>
        /// Profiling save of an asset.
        /// </summary>
        public static readonly ProfilingKey ContentSave = new ProfilingKey(Content, "Save", ProfilingKeyFlags.Log);
    }
}
