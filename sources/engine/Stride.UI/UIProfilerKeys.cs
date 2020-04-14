// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Diagnostics;

namespace Stride.UI
{
    /// <summary>
    /// Various <see cref="ProfilingKey"/> used to measure performance across some part of the UI system.
    /// </summary>
    public static class UIProfilerKeys
    {
        public static readonly ProfilingKey UI = new ProfilingKey("UI");

        public static readonly ProfilingKey TouchEventsUpdate = new ProfilingKey(UI, "TouchEvents");
    }
}
