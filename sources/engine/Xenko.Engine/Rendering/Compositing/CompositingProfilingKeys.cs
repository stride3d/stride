// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Diagnostics;

namespace Xenko.Rendering.Compositing
{
    public class CompositingProfilingKeys
    {
        public static readonly ProfilingKey Compositing = new ProfilingKey("Compositing");

        public static readonly ProfilingKey Opaque = new ProfilingKey(Compositing, "Opaque");

        public static readonly ProfilingKey Transparent = new ProfilingKey(Compositing, "Transparent");

        public static readonly ProfilingKey MsaaResolve = new ProfilingKey(Compositing, "MSAA Resolve");

        public static readonly ProfilingKey LightShafts = new ProfilingKey(Compositing, "LightShafts");

        public static readonly ProfilingKey GBuffer = new ProfilingKey(Compositing, "GBuffer");
    }
}
