// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Collections;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Compositing
{
    /// <summary>
    /// Represents how we setup the graphics pipeline output targets.
    /// </summary>
    public sealed class RenderOutputValidator
    {
        private readonly FastList<RenderTargetDescription> renderTargets = new FastList<RenderTargetDescription>();
        private readonly RenderStage renderStage;

        private int validatedTargetCount;
        private bool hasChanged;
        private MultisampleCount multisampleCount;
        private PixelFormat depthStencilFormat;

        public IReadOnlyList<RenderTargetDescription> RenderTargets => renderTargets;

        public ShaderMixinSource ShaderSource { get; private set; }

        //public RenderOutputDescription Output { get; private set; }

        internal RenderOutputValidator(RenderStage renderStage)
        {
            this.renderStage = renderStage;
        }

        public void Add<T>(PixelFormat format, bool isShaderResource = true)
            where T : IRenderTargetSemantic, new()
        {
            var description = new RenderTargetDescription
            {
                Semantic = new T(),
                Format = format,
            };

            int index = validatedTargetCount++;
            if (index < renderTargets.Count)
            {
                if (renderTargets[index] != description)
                    hasChanged = true;

                renderTargets[index] = description;
            }
            else
            {
                renderTargets.Add(description);
                hasChanged = true;
            }
        }

        public void BeginCustomValidation(PixelFormat depthStencilFormat, MultisampleCount multisampleCount = MultisampleCount.None)
        {
            validatedTargetCount = 0;
            hasChanged = false;

            if (this.depthStencilFormat != depthStencilFormat)
            {
                hasChanged = true;
                this.depthStencilFormat = depthStencilFormat;
            }
            if (this.multisampleCount != multisampleCount)
            {
                hasChanged = true;
                this.multisampleCount = multisampleCount;
            }
        }

        public unsafe void EndCustomValidation()
        {
            if (validatedTargetCount < renderTargets.Count || hasChanged)
            {
                renderTargets.Resize(validatedTargetCount, false);

                // Recalculate shader sources
                ShaderSource = new ShaderMixinSource();
                ShaderSource.Macros.Add(new ShaderMacro("XENKO_RENDER_TARGET_COUNT", renderTargets.Count));
                for (var index = 0; index < renderTargets.Count; index++)
                {
                    var renderTarget = renderTargets[index];
                    if (index > 0)
                        ShaderSource.Compositions.Add($"ShadingColor{index}", renderTarget.Semantic.ShaderClass);
                }

                ShaderSource.Macros.Add(new ShaderMacro("XENKO_MULTISAMPLE_COUNT", (int)multisampleCount));
            }

            renderStage.Output.RenderTargetCount = renderTargets.Count;
            renderStage.Output.MultisampleCount = multisampleCount;
            renderStage.Output.DepthStencilFormat = depthStencilFormat;

            fixed (PixelFormat* formats = &renderStage.Output.RenderTargetFormat0)
            {
                for (int i = 0; i < renderTargets.Count; ++i)
                {
                    formats[i] = renderTargets[i].Format;
                }
            }
        }

        public void Validate(ref RenderOutputDescription renderOutput)
        {
            hasChanged = false;
            if (multisampleCount != renderOutput.MultisampleCount)
            {
                hasChanged = true;
                multisampleCount = renderOutput.MultisampleCount;
            }

            if (hasChanged)
            {
                // Recalculate shader sources
                ShaderSource = new ShaderMixinSource();
                ShaderSource.Macros.Add(new ShaderMacro("XENKO_MULTISAMPLE_COUNT", (int)multisampleCount));
            }

            renderStage.Output = renderOutput;
        }

        public int Find(Type semanticType)
        {
            for (int index = 0; index < renderTargets.Count; index++)
            {
                if (renderTargets[index].Semantic.GetType() == semanticType)
                    return index;
            }

            return -1;
        }

        public int Find<T>()
            where T : IRenderTargetSemantic
        {
            return Find(typeof(T));
        }
    }
}
