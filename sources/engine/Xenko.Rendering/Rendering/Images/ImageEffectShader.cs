// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Post effect using an <see cref="Effect"/> (either xkfx or xksl).
    /// </summary>
    [DataContract("ImageEffectShader")]
    public class ImageEffectShader : ImageEffect
    {
        private MutablePipelineState pipelineState;
        private bool pipelineStateDirty = true;
        private BlendStateDescription blendState = BlendStateDescription.Default;
        private DepthStencilStateDescription depthStencilState = DepthStencilStateDescription.Default;
        private EffectBytecode previousBytecode;
        private bool delaySetRenderTargets;

        [DataMemberIgnore]
        public BlendStateDescription BlendState
        {
            get { return blendState; }
            set { blendState = value; pipelineStateDirty = true; }
        }

        [DataMemberIgnore]
        public DepthStencilStateDescription DepthStencilState
        {
            get { return depthStencilState; }
            set { depthStencilState = value; pipelineStateDirty = true; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageEffectShader" /> class.
        /// </summary>
        public ImageEffectShader(string effectName = null, bool delaySetRenderTargets = false)
        {
            EffectInstance = new DynamicEffectInstance(effectName, Parameters);
            EnableSetRenderTargets = !delaySetRenderTargets;
            this.delaySetRenderTargets = delaySetRenderTargets;
            if (effectName != null)
                Name = effectName;
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            pipelineState = new MutablePipelineState(Context.GraphicsDevice);
            pipelineState.State.SetDefaults();
            pipelineState.State.InputElements = PrimitiveQuad.VertexDeclaration.CreateInputElements();
            pipelineState.State.PrimitiveType = PrimitiveQuad.PrimitiveType;

            if (EffectName == null) throw new ArgumentNullException("No EffectName specified");

            // Setup the effect compiler
            EffectInstance.Initialize(Context.Services);

            // We give ImageEffectShader a higher priority, since they are usually executed serially and blocking
            EffectInstance.EffectCompilerParameters.TaskPriority = -1;

            SetDefaultParameters();
        }

        /// <summary>
        /// The current effect instance.
        /// </summary>
        [DataMemberIgnore]
        public DynamicEffectInstance EffectInstance { get; private set; }

        /// <summary>
        /// Effect name.
        /// </summary>
        [DataMemberIgnore]
        public string EffectName
        {
            get { return EffectInstance.EffectName; }
            protected set { EffectInstance.EffectName = value; }
        }

        /// <summary>
        /// Sets the default parameters (called at constructor time and if <see cref="Reset"/> is called)
        /// </summary>
        protected override void SetDefaultParameters()
        {
            // TODO: Do not use slow version
            Parameters.Set(TexturingKeys.Sampler, GraphicsDevice.SamplerStates.LinearClamp);
        }

        protected override void PreDrawCore(RenderDrawContext context)
        {
            base.PreDrawCore(context);

            // Default handler for parameters
            UpdateParameters();
        }

        /// <summary>
        /// Updates the effect <see cref="ImageEffectShader.Parameters" /> from properties defined in this instance. See remarks.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Expecting less than 10 textures in input</exception>
        /// <remarks>By default, all the input textures will be remapped to <see cref="TexturingKeys.Texture0" />...etc.</remarks>
        protected virtual void UpdateParameters()
        {
            // By default, we are copying all input textures to TexturingKeys.Texture#
            var count = InputCount;
            for (int i = 0; i < count; i++)
            {
                var texture = GetInput(i);
                if (i < TexturingKeys.DefaultTextures.Count)
                {
                    var texturingKeys = texture.ViewDimension == TextureDimension.TextureCube ? TexturingKeys.TextureCubes : TexturingKeys.DefaultTextures;
                    // TODO GRAPHICS REFACTOR Do not use slow version
                    Parameters.Set(texturingKeys[i], texture);
                    Parameters.Set(TexturingKeys.TexturesTexelSize[i], new Vector2(1.0f / texture.ViewWidth, 1.0f / texture.ViewHeight));
                }
                else
                {
                    throw new InvalidOperationException("Expecting less than {0} textures in input".ToFormat(TexturingKeys.DefaultTextures.Count));
                }
            }
        }

        protected override unsafe void DrawCore(RenderDrawContext context)
        {
            // Clear render targets if there is a dependency conflict (D3D11 warning)
            if (delaySetRenderTargets)
                context.CommandList.ResetTargets();

            if (EffectInstance.UpdateEffect(GraphicsDevice) || pipelineStateDirty || previousBytecode != EffectInstance.Effect.Bytecode)
            {
                // The EffectInstance might have been updated from outside
                previousBytecode = EffectInstance.Effect.Bytecode;

                pipelineState.State.RootSignature = EffectInstance.RootSignature;
                pipelineState.State.EffectBytecode = EffectInstance.Effect.Bytecode;
                pipelineState.State.BlendState = blendState;
                pipelineState.State.DepthStencilState = depthStencilState;

                var renderTargetCount = OutputCount;
                if (renderTargetCount > 0)
                {
                    // Special case: texture cube
                    var isTextureCube = GetOutput(0).ViewDimension == TextureDimension.TextureCube;
                    if (isTextureCube)
                    {
                        renderTargetCount = 6;
                    }

                    // Capture output state manually (since render targets might not be bound if delaySetRenderTargets is set to true)
                    pipelineState.State.Output.RenderTargetCount = renderTargetCount;
                    fixed (PixelFormat* pixelFormatStart = &pipelineState.State.Output.RenderTargetFormat0)
                    for (int i = 0; i < renderTargetCount; ++i)
                    {
                        pixelFormatStart[i] = GetOutput(isTextureCube ? 0 : i).ViewFormat;
                    }
                }

                if (HasDepthStencilOutput)
                    pipelineState.State.Output.DepthStencilFormat = DepthStencil.Format;

                pipelineState.Update();
                pipelineStateDirty = false;
            }

            context.CommandList.SetPipelineState(pipelineState.CurrentState);

            EffectInstance.Apply(context.GraphicsContext);

            // Now that resources are bound, set render targets
            if (delaySetRenderTargets)
                SetRenderTargets(context);

            // Draw a full screen quad
            context.GraphicsDevice.PrimitiveQuad.Draw(context.CommandList);
        }
    }
}
