// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;

namespace Xenko.Rendering.Sprites
{
    public class SpriteRenderFeature : RootRenderFeature
    {
        private ThreadLocal<ThreadContext> threadContext;

        private Dictionary<BlendModes, BlendStateDescription> blendModeToDescription = new Dictionary<BlendModes, BlendStateDescription>();
        private Dictionary<SpriteBlend, BlendModes> spriteBlendToBlendMode = new Dictionary<SpriteBlend, BlendModes>();

        public override Type SupportedRenderObjectType => typeof(RenderSprite);

        private enum BlendModes
        {
            Default = 0,
            Additive = 1,
            Alpha = 2,
            NonPremultiplied = 3,
            NoColor = 4,
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            threadContext = new ThreadLocal<ThreadContext>(() => new ThreadContext(Context.GraphicsDevice), true);

            spriteBlendToBlendMode[SpriteBlend.None] = BlendModes.Default;
            spriteBlendToBlendMode[SpriteBlend.AdditiveBlend] = BlendModes.Additive;
            spriteBlendToBlendMode[SpriteBlend.NoColor] = BlendModes.NoColor;

            blendModeToDescription[BlendModes.Default] = BlendStates.Default;
            blendModeToDescription[BlendModes.Additive] = BlendStates.Additive;
            blendModeToDescription[BlendModes.Alpha] = BlendStates.AlphaBlend;
            blendModeToDescription[BlendModes.NonPremultiplied] = BlendStates.NonPremultiplied;
            blendModeToDescription[BlendModes.NoColor] = BlendStates.ColorDisabled;
        }

        protected override void Destroy()
        {
            if (threadContext == null)
            {
                return;
            }

            base.Destroy();

            foreach (var context in threadContext.Values)
            {
                context.Dispose();
            }
            threadContext.Dispose();
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            base.Draw(context, renderView, renderViewStage, startIndex, endIndex);

            var isMultisample = RenderSystem.RenderStages[renderViewStage.Index].Output.MultisampleCount != MultisampleCount.None;

            var batchContext = threadContext.Value;

            Matrix viewInverse;
            Matrix.Invert(ref renderView.View, out viewInverse);

            uint previousBatchState = uint.MaxValue;

            //TODO string comparison ...?
            var isPicking = RenderSystem.RenderStages[renderViewStage.Index].Name == "Picking";

            bool hasBegin = false;
            for (var index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = GetRenderNode(renderNodeReference);

                var renderSprite = (RenderSprite)renderNode.RenderObject;

                var sprite = renderSprite.Sprite;
                if (sprite == null)
                    continue;

                // TODO: this should probably be moved to Prepare()
                // Project the position
                // TODO: This could be done in a SIMD batch, but we need to figure-out how to plugin in with RenderMesh object
                var worldPosition = new Vector4(renderSprite.WorldMatrix.TranslationVector, 1.0f);

                Vector4 projectedPosition;
                Vector4.Transform(ref worldPosition, ref renderView.ViewProjection, out projectedPosition);
                var projectedZ = projectedPosition.Z / projectedPosition.W;
                
                BlendModes blendMode;
                EffectInstance currentEffect = null;
                if (isPicking)
                {
                    blendMode = BlendModes.Default;
                    currentEffect = batchContext.GetOrCreatePickingSpriteEffect(RenderSystem.EffectSystem);
                }
                else
                {
                    var spriteBlend = renderSprite.BlendMode;
                    if (spriteBlend == SpriteBlend.Auto)
                        spriteBlend = sprite.IsTransparent ? SpriteBlend.AlphaBlend : SpriteBlend.None;

                    if (spriteBlend == SpriteBlend.AlphaBlend)
                    {
                        blendMode = renderSprite.PremultipliedAlpha ? BlendModes.Alpha : BlendModes.NonPremultiplied;
                    }
                    else
                    {
                        blendMode = spriteBlendToBlendMode[spriteBlend];
                    }
                }

                // Check if the current blend state has changed in any way, if not
                // Note! It doesn't really matter in what order we build the bitmask, the result is not preserved anywhere except in this method
                var currentBatchState = (uint)blendMode;
                currentBatchState = (currentBatchState << 1) + (renderSprite.IgnoreDepth ? 1U : 0U);
                currentBatchState = (currentBatchState << 1) + (renderSprite.IsAlphaCutoff ? 1U : 0U);
                currentBatchState = (currentBatchState << 2) + ((uint)renderSprite.Sampler);

                if (previousBatchState != currentBatchState)
                {
                    var blendState = blendModeToDescription[blendMode];

                    if (renderSprite.IsAlphaCutoff)
                        currentEffect = batchContext.GetOrCreateAlphaCutoffSpriteEffect(RenderSystem.EffectSystem);

                    var depthStencilState = renderSprite.IgnoreDepth ? DepthStencilStates.None : DepthStencilStates.Default;

                    var samplerState = context.GraphicsDevice.SamplerStates.LinearClamp;
                    if (renderSprite.Sampler != SpriteSampler.LinearClamp)
                    {
                        switch (renderSprite.Sampler)
                        {
                            case SpriteSampler.PointClamp:
                                samplerState = context.GraphicsDevice.SamplerStates.PointClamp;
                                break;
                            case SpriteSampler.AnisotropicClamp:
                                samplerState = context.GraphicsDevice.SamplerStates.AnisotropicClamp;
                                break;
                        }
                    }

                    if (hasBegin)
                    {
                        batchContext.SpriteBatch.End();
                    }

                    var rasterizerState = RasterizerStates.CullNone;
                    if (isMultisample)
                    {
                        rasterizerState.MultisampleCount = RenderSystem.RenderStages[renderViewStage.Index].Output.MultisampleCount;
                        rasterizerState.MultisampleAntiAliasLine = true;
                    }

                    batchContext.SpriteBatch.Begin(context.GraphicsContext, renderView.ViewProjection, SpriteSortMode.Deferred, blendState, samplerState, depthStencilState, rasterizerState, currentEffect);
                    hasBegin = true;
                }
                previousBatchState = currentBatchState;

                var sourceRegion = sprite.Region;
                var texture = sprite.Texture;
                var color = renderSprite.Color;                
                if (isPicking) // TODO move this code corresponding to picking out of the runtime code.
                {
                    var compId = RuntimeIdHelper.ToRuntimeId(renderSprite.Source);
                    color = new Color4(compId, 0.0f, 0.0f, 0.0f);
                }

                // skip the sprite if no texture is set.
                if (texture == null)
                    continue;

                // determine the element world matrix depending on the type of sprite
                var worldMatrix = renderSprite.WorldMatrix;
                if (renderSprite.SpriteType == SpriteType.Billboard)
                {
                    worldMatrix = viewInverse;

                    // remove scale of the camera
                    worldMatrix.Row1 /= ((Vector3)viewInverse.Row1).Length();
                    worldMatrix.Row2 /= ((Vector3)viewInverse.Row2).Length();

                    // set the scale of the object
                    worldMatrix.Row1 *= ((Vector3)renderSprite.WorldMatrix.Row1).Length();
                    worldMatrix.Row2 *= ((Vector3)renderSprite.WorldMatrix.Row2).Length();

                    // set the position
                    worldMatrix.TranslationVector = renderSprite.WorldMatrix.TranslationVector;

                    // set the rotation
                    var localRotationZ = renderSprite.RotationEulerZ;
                    if (localRotationZ != 0)
                        worldMatrix = Matrix.RotationZ(localRotationZ) * worldMatrix;
                }

                // calculate normalized position of the center of the sprite (takes into account the possible rotation of the image)
                var normalizedCenter = new Vector2(sprite.Center.X / sourceRegion.Width - 0.5f, 0.5f - sprite.Center.Y / sourceRegion.Height);
                if (sprite.Orientation == ImageOrientation.Rotated90)
                {
                    var oldCenterX = normalizedCenter.X;
                    normalizedCenter.X = -normalizedCenter.Y;
                    normalizedCenter.Y = oldCenterX;
                }
                // apply the offset due to the center of the sprite
                var centerOffset = Vector2.Modulate(normalizedCenter, sprite.SizeInternal);
                worldMatrix.M41 -= centerOffset.X * worldMatrix.M11 + centerOffset.Y * worldMatrix.M21;
                worldMatrix.M42 -= centerOffset.X * worldMatrix.M12 + centerOffset.Y * worldMatrix.M22;
                worldMatrix.M43 -= centerOffset.X * worldMatrix.M13 + centerOffset.Y * worldMatrix.M23;

                // adapt the source region to match what is expected at full resolution
                if (texture.ViewType == ViewType.Full && texture.ViewWidth != texture.FullQualitySize.Width)
                {
                    var fullQualitySize = texture.FullQualitySize;
                    var horizontalRatio = texture.ViewWidth / (float)fullQualitySize.Width;
                    var verticalRatio = texture.ViewHeight / (float)fullQualitySize.Height;
                    sourceRegion.X *= horizontalRatio;
                    sourceRegion.Width *= horizontalRatio;
                    sourceRegion.Y *= verticalRatio;
                    sourceRegion.Height *= verticalRatio;
                }

                // register resource usage.
                Context.StreamingManager?.StreamResources(texture);

                // draw the sprite
                batchContext.SpriteBatch.Draw(texture, ref worldMatrix, ref sourceRegion, ref sprite.SizeInternal, ref color, sprite.Orientation, renderSprite.Swizzle, projectedZ);
            }

            if (hasBegin)
            {
                batchContext.SpriteBatch.End();
            }
        }

        private class ThreadContext : IDisposable
        {
            private bool isSrgb;
            private EffectInstance pickingEffect;
            private EffectInstance alphaCutoffEffect;

            public Sprite3DBatch SpriteBatch { get; }

            public ThreadContext(GraphicsDevice device)
            {
                isSrgb = device.ColorSpace == ColorSpace.Gamma;
                SpriteBatch = new Sprite3DBatch(device);
            }

            public EffectInstance GetOrCreatePickingSpriteEffect(EffectSystem effectSystem)
            {
                return pickingEffect ?? (pickingEffect = new EffectInstance(effectSystem.LoadEffect("SpritePicking").WaitForResult()));
            }

            public EffectInstance GetOrCreateAlphaCutoffSpriteEffect(EffectSystem effectSystem)
            {
                if (alphaCutoffEffect != null)
                    return alphaCutoffEffect;

                alphaCutoffEffect = new EffectInstance(effectSystem.LoadEffect("SpriteAlphaCutoffEffect").WaitForResult());
                alphaCutoffEffect.Parameters.Set(SpriteBaseKeys.ColorIsSRgb, isSrgb);

                return alphaCutoffEffect;
            }

            public void Dispose()
            {
                SpriteBatch.Dispose();
            }
        }
    }
}
