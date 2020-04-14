// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL 
using System;
using OpenTK.Graphics;
#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Stride.Graphics
{
    class BlendState
    {
        internal readonly ColorWriteChannels ColorWriteChannels;

        private readonly bool blendEnable;
        private readonly BlendEquationMode blendEquationModeColor;
        private readonly BlendEquationMode blendEquationModeAlpha;
        private readonly BlendingFactorSrc blendFactorSrcColor;
        private readonly BlendingFactorSrc blendFactorSrcAlpha;
        private readonly BlendingFactorDest blendFactorDestColor;
        private readonly BlendingFactorDest blendFactorDestAlpha;
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
            blendFactorDestColor = (BlendingFactorDest)ToOpenGL(blendStateDescription.RenderTarget0.ColorDestinationBlend);
            blendFactorDestAlpha = (BlendingFactorDest)ToOpenGL(blendStateDescription.RenderTarget0.AlphaDestinationBlend);

            blendEquationHash = (uint)blendStateDescription.RenderTarget0.ColorBlendFunction
                             | ((uint)blendStateDescription.RenderTarget0.AlphaBlendFunction << 8);

            blendFuncHash = (uint)blendStateDescription.RenderTarget0.ColorSourceBlend
                         | ((uint)blendStateDescription.RenderTarget0.AlphaSourceBlend << 8)
                         | ((uint)blendStateDescription.RenderTarget0.ColorDestinationBlend << 16)
                         | ((uint)blendStateDescription.RenderTarget0.AlphaDestinationBlend << 24);
        }

        public static BlendEquationMode ToOpenGL(BlendFunction blendFunction)
        {
            switch (blendFunction)
            {
                case BlendFunction.Subtract:
                    return BlendEquationMode.FuncSubtract;
                case BlendFunction.Add:
                    return BlendEquationMode.FuncAdd;
#if !STRIDE_GRAPHICS_API_OPENGLES
                case BlendFunction.Max:
                    return BlendEquationMode.Max;
                case BlendFunction.Min:
                    return BlendEquationMode.Min;
#endif
                case BlendFunction.ReverseSubtract:
                    return BlendEquationMode.FuncReverseSubtract;
                default:
                    throw new NotSupportedException();
            }
        }

        public static BlendingFactorSrc ToOpenGL(Blend blend)
        {
            switch (blend)
            {
                case Blend.Zero:
                    return BlendingFactorSrc.Zero;
                case Blend.One:
                    return BlendingFactorSrc.One;
                case Blend.SourceColor:
                    return (BlendingFactorSrc)BlendingFactorDest.SrcColor;
                case Blend.InverseSourceColor:
                    return (BlendingFactorSrc)BlendingFactorDest.OneMinusSrcColor;
                case Blend.SourceAlpha:
                    return BlendingFactorSrc.SrcAlpha;
                case Blend.InverseSourceAlpha:
                    return BlendingFactorSrc.OneMinusSrcAlpha;
                case Blend.DestinationAlpha:
                    return BlendingFactorSrc.DstAlpha;
                case Blend.InverseDestinationAlpha:
                    return BlendingFactorSrc.OneMinusDstAlpha;
                case Blend.DestinationColor:
                    return BlendingFactorSrc.DstColor;
                case Blend.InverseDestinationColor:
                    return BlendingFactorSrc.OneMinusDstColor;
                case Blend.SourceAlphaSaturate:
                    return BlendingFactorSrc.SrcAlphaSaturate;
                case Blend.BlendFactor:
                    return BlendingFactorSrc.ConstantColor;
                case Blend.InverseBlendFactor:
                    return BlendingFactorSrc.OneMinusConstantColor;
                case Blend.SecondarySourceColor:
                case Blend.InverseSecondarySourceColor:
                case Blend.SecondarySourceAlpha:
                case Blend.InverseSecondarySourceAlpha:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException("blend");
            }
        }

        public void Apply(CommandList commandList, BlendState oldBlendState)
        {
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
                RestoreColorMask();
            }

            if (!blendEnable && oldBlendState.blendEnable)
                GL.Disable(EnableCap.Blend);
        }

        internal void RestoreColorMask()
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
