// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL 
using System;

namespace Stride.Graphics
{
    class BlendState
    {
        internal readonly ColorWriteChannels ColorWriteChannels;

        private readonly bool blendEnable;
        private readonly BlendEquationModeEXT blendEquationModeColor;
        private readonly BlendEquationModeEXT blendEquationModeAlpha;
        private readonly BlendingFactor blendFactorSrcColor;
        private readonly BlendingFactor blendFactorSrcAlpha;
        private readonly BlendingFactor blendFactorDestColor;
        private readonly BlendingFactor blendFactorDestAlpha;
        private readonly uint blendEquationHash;
        private readonly uint blendFuncHash;

        internal unsafe BlendState(BlendStateDescription blendStateDescription, bool hasRenderTarget)
        {
            var renderTargets = &blendStateDescription.RenderTarget0;
            for (int i = 1; i < 8; ++i)
            {
                if (renderTargets[i].BlendEnable || renderTargets[i].ColorWriteChannels != ColorWriteChannels.All)
                    throw new NotSupportedException();
            }

            ColorWriteChannels = blendStateDescription.RenderTarget0.ColorWriteChannels;
            if (!hasRenderTarget)
                ColorWriteChannels = 0;

            blendEnable = blendStateDescription.RenderTarget0.BlendEnable;

            blendEquationModeColor = ToOpenGL(blendStateDescription.RenderTarget0.ColorBlendFunction);
            blendEquationModeAlpha = ToOpenGL(blendStateDescription.RenderTarget0.AlphaBlendFunction);
            blendFactorSrcColor = ToOpenGL(blendStateDescription.RenderTarget0.ColorSourceBlend);
            blendFactorSrcAlpha = ToOpenGL(blendStateDescription.RenderTarget0.AlphaSourceBlend);
            blendFactorDestColor = ToOpenGL(blendStateDescription.RenderTarget0.ColorDestinationBlend);
            blendFactorDestAlpha = ToOpenGL(blendStateDescription.RenderTarget0.AlphaDestinationBlend);

            blendEquationHash = (uint)blendStateDescription.RenderTarget0.ColorBlendFunction
                             | ((uint)blendStateDescription.RenderTarget0.AlphaBlendFunction << 8);

            blendFuncHash = (uint)blendStateDescription.RenderTarget0.ColorSourceBlend
                         | ((uint)blendStateDescription.RenderTarget0.AlphaSourceBlend << 8)
                         | ((uint)blendStateDescription.RenderTarget0.ColorDestinationBlend << 16)
                         | ((uint)blendStateDescription.RenderTarget0.AlphaDestinationBlend << 24);
        }

        public static BlendEquationModeEXT ToOpenGL(BlendFunction blendFunction)
        {
            switch (blendFunction)
            {
                case BlendFunction.Subtract:
                    return BlendEquationModeEXT.FuncSubtract;
                case BlendFunction.Add:
                    return BlendEquationModeEXT.FuncAdd;
                case BlendFunction.Max:
                    return BlendEquationModeEXT.Max;
                case BlendFunction.Min:
                    return BlendEquationModeEXT.Min;
                case BlendFunction.ReverseSubtract:
                    return BlendEquationModeEXT.FuncReverseSubtract;
                default:
                    throw new NotSupportedException();
            }
        }

        public static BlendingFactor ToOpenGL(Blend blend)
        {
            switch (blend)
            {
                case Blend.Zero:
                    return BlendingFactor.Zero;
                case Blend.One:
                    return BlendingFactor.One;
                case Blend.SourceColor:
                    return BlendingFactor.SrcColor;
                case Blend.InverseSourceColor:
                    return BlendingFactor.OneMinusSrcColor;
                case Blend.SourceAlpha:
                    return BlendingFactor.SrcAlpha;
                case Blend.InverseSourceAlpha:
                    return BlendingFactor.OneMinusSrcAlpha;
                case Blend.DestinationAlpha:
                    return BlendingFactor.DstAlpha;
                case Blend.InverseDestinationAlpha:
                    return BlendingFactor.OneMinusDstAlpha;
                case Blend.DestinationColor:
                    return BlendingFactor.DstColor;
                case Blend.InverseDestinationColor:
                    return BlendingFactor.OneMinusDstColor;
                case Blend.SourceAlphaSaturate:
                    return BlendingFactor.SrcAlphaSaturate;
                case Blend.BlendFactor:
                    return BlendingFactor.ConstantColor;
                case Blend.InverseBlendFactor:
                    return BlendingFactor.OneMinusConstantColor;
                case Blend.SecondarySourceColor:
                case Blend.InverseSecondarySourceColor:
                case Blend.SecondarySourceAlpha:
                case Blend.InverseSecondarySourceAlpha:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException("blend");
            }
        }

        internal void Apply(CommandList commandList, BlendState oldBlendState)
        {
            var GL = commandList.GL;
            // note: need to update blend equation, blend function and color mask even when the blend state is disable in order to keep the hash based caching system valid

            if (blendEnable && !oldBlendState.blendEnable)
                GL.Enable(EnableCap.Blend);

            if (blendEquationHash != oldBlendState.blendEquationHash)
                GL.BlendEquationSeparate(blendEquationModeColor, blendEquationModeAlpha);

            if (blendFuncHash != oldBlendState.blendFuncHash)
                GL.BlendFuncSeparate(blendFactorSrcColor, blendFactorDestColor, blendFactorSrcAlpha, blendFactorDestAlpha);

            if (commandList.NewBlendFactor != commandList.BoundBlendFactor)
            {
                commandList.BoundBlendFactor = commandList.NewBlendFactor;
                GL.BlendColor(commandList.NewBlendFactor.R, commandList.NewBlendFactor.G, commandList.NewBlendFactor.B, commandList.NewBlendFactor.A);
            }

            if (ColorWriteChannels != oldBlendState.ColorWriteChannels)
            {
                RestoreColorMask(GL);
            }

            if (!blendEnable && oldBlendState.blendEnable)
                GL.Disable(EnableCap.Blend);
        }

        internal void RestoreColorMask(GL GL)
        {
            GL.ColorMask(
                (ColorWriteChannels & ColorWriteChannels.Red) != 0,
                (ColorWriteChannels & ColorWriteChannels.Green) != 0,
                (ColorWriteChannels & ColorWriteChannels.Blue) != 0,
                (ColorWriteChannels & ColorWriteChannels.Alpha) != 0);
        }
    }
} 
#endif
