// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Diagnostics;

namespace Xenko.Rendering
{
    /// <summary>
    /// Various <see cref="ProfilingKey"/> used to measure performance across some part of the effect system.
    /// </summary>
    public class ProfilingKeys
    {
        public static readonly ProfilingKey Engine = new ProfilingKey("Engine");

        public static readonly ProfilingKey ModelRenderProcessor = new ProfilingKey(Engine, "ModelRenderer");

        public static readonly ProfilingKey PrepareMesh = new ProfilingKey(ModelRenderProcessor, "PrepareMesh");

        public static readonly ProfilingKey RenderMesh = new ProfilingKey(ModelRenderProcessor, "RenderMesh");

        public static readonly ProfilingKey AnimationProcessor = new ProfilingKey(Engine, "AnimationProcessor");
    }
}
