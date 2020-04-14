// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Diagnostics;

namespace Stride.Games
{
    /// <summary>
    /// Profiling keys for <see cref="GraphicsDeviceManager"/>.
    /// </summary>
    public static class GraphicsDeviceManagerProfilingKeys
    {
        public static readonly ProfilingKey GraphicsDeviceManager = new ProfilingKey("GraphicsDeviceManager");

        /// <summary>
        /// Profiling graphics device initialization.
        /// </summary>
        public static readonly ProfilingKey CreateDevice = new ProfilingKey(GraphicsDeviceManager, "CreateGraphicsDevice");
    }
}
