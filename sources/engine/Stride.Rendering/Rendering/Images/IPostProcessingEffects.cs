// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Graphics;
using Stride.Rendering.Compositing;

namespace Stride.Rendering.Images
{
    public interface IPostProcessingEffects : ISharedRenderer, IDisposable
    {
        void Collect(RenderContext context);

        void Draw(RenderDrawContext drawContext, RenderOutputValidator outputValidator, Texture[] inputs, Texture inputDepthStencil, Texture outputTarget);

        bool RequiresVelocityBuffer { get; }

        bool RequiresNormalBuffer { get; }

        bool RequiresSpecularRoughnessBuffer { get; }
    }
}
