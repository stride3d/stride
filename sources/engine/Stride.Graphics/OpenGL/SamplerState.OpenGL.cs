// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_OPENGL 
using System;
using Xenko.Core.Mathematics;
#if XENKO_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
using TextureCompareMode = OpenTK.Graphics.ES30.All;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Xenko.Graphics
{
    public partial class SamplerState
    {
        private const TextureFilter AnisotropicMask = TextureFilter.Anisotropic & ~TextureFilter.Linear;
        private const TextureFilter ComparisonMask = TextureFilter.ComparisonLinear & ~TextureFilter.Linear;

        private TextureWrapMode textureWrapS;
        private TextureWrapMode textureWrapT;
        private TextureWrapMode textureWrapR;

        private TextureMinFilter minFilter;
        private TextureMagFilter magFilter;
#if XENKO_GRAPHICS_API_OPENGLES
        private TextureMinFilter minFilterNoMipmap;
#endif

        private int maxAnisotropy;

        private float[] borderColor;

        private DepthFunction compareFunc;
        private TextureCompareMode compareMode;

        private SamplerState(GraphicsDevice device, SamplerStateDescription samplerStateDescription) : base(device)
        {
            Description = samplerStateDescription;

            textureWrapS = samplerStateDescription.AddressU.ToOpenGL();
            textureWrapT = samplerStateDescription.AddressV.ToOpenGL();
            textureWrapR = samplerStateDescription.AddressW.ToOpenGL();

            compareMode = TextureCompareMode.None;

            // ComparisonPoint can act as a mask for Comparison filters (0x80)
            if ((samplerStateDescription.Filter & ComparisonMask) != 0)
                compareMode = TextureCompareMode.CompareRefToTexture;

            compareFunc = samplerStateDescription.CompareFunction.ToOpenGLDepthFunction();
            borderColor = samplerStateDescription.BorderColor.ToArray();
            // TODO: How to do MipLinear vs MipPoint?
            switch (samplerStateDescription.Filter & ~(ComparisonMask | AnisotropicMask)) // Ignore comparison (128) and anisotropic (64) part
            {
                case TextureFilter.MinMagLinearMipPoint:
                    minFilter = TextureMinFilter.LinearMipmapNearest;
                    magFilter = TextureMagFilter.Linear;
                    break;
                case TextureFilter.Linear:
                    minFilter = TextureMinFilter.LinearMipmapLinear;
                    magFilter = TextureMagFilter.Linear;
                    break;
                case TextureFilter.MinPointMagMipLinear:
                    minFilter = TextureMinFilter.NearestMipmapLinear;
                    magFilter = TextureMagFilter.Linear;
                    break;
                case TextureFilter.Point:
                    minFilter = TextureMinFilter.NearestMipmapNearest;
                    magFilter = TextureMagFilter.Nearest;
                    break;
                case TextureFilter.MinPointMagLinearMipPoint:
                    minFilter = TextureMinFilter.NearestMipmapNearest;
                    magFilter = TextureMagFilter.Linear;
                    break;
                case TextureFilter.MinLinearMagMipPoint:
                    minFilter = TextureMinFilter.LinearMipmapNearest;
                    magFilter = TextureMagFilter.Nearest;
                    break;
                case TextureFilter.MinMagPointMipLinear:
                    minFilter = TextureMinFilter.NearestMipmapLinear;
                    magFilter = TextureMagFilter.Nearest;
                    break;
                case TextureFilter.MinLinearMagPointMipLinear:
                    minFilter = TextureMinFilter.LinearMipmapLinear;
                    magFilter = TextureMagFilter.Nearest;
                    break;
                default:
                    throw new NotImplementedException();
            }

            maxAnisotropy = ((samplerStateDescription.Filter & AnisotropicMask) != 0) ? Description.MaxAnisotropy : 1;

#if XENKO_GRAPHICS_API_OPENGLES
            // On OpenGL ES, we need to choose the appropriate min filter ourself if the texture doesn't contain mipmaps (done at PreDraw)
            minFilterNoMipmap = minFilter;
            if (minFilterNoMipmap == TextureMinFilter.LinearMipmapLinear)
                minFilterNoMipmap = TextureMinFilter.Linear;
            else if (minFilterNoMipmap == TextureMinFilter.NearestMipmapLinear)
                minFilterNoMipmap = TextureMinFilter.Nearest;
#endif
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();
            return true;
        }

        internal void Apply(bool hasMipmap, SamplerState oldSamplerState, TextureTarget target)
        {
            if (Description.MinMipLevel != oldSamplerState.Description.MinMipLevel)
                GL.TexParameter(target, TextureParameterName.TextureMinLod, Description.MinMipLevel);
            if (Description.MaxMipLevel != oldSamplerState.Description.MaxMipLevel)
                GL.TexParameter(target, TextureParameterName.TextureMaxLod, Description.MaxMipLevel);
            if (textureWrapR != oldSamplerState.textureWrapR)
                GL.TexParameter(target, TextureParameterName.TextureWrapR, (int)textureWrapR);
            if (compareMode != oldSamplerState.compareMode)
                GL.TexParameter(target, TextureParameterName.TextureCompareMode, (int)compareMode);
            if (compareFunc != oldSamplerState.compareFunc)
                GL.TexParameter(target, TextureParameterName.TextureCompareFunc, (int)compareFunc);

#if !XENKO_GRAPHICS_API_OPENGLES
            if (borderColor != oldSamplerState.borderColor)
                GL.TexParameter(target, TextureParameterName.TextureBorderColor, borderColor);
            if (Description.MipMapLevelOfDetailBias != oldSamplerState.Description.MipMapLevelOfDetailBias)
                GL.TexParameter(target, TextureParameterName.TextureLodBias, Description.MipMapLevelOfDetailBias);
            if (minFilter != oldSamplerState.minFilter)
                GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)minFilter);
#else
            // On OpenGL ES, we need to choose the appropriate min filter ourself if the texture doesn't contain mipmaps (done at PreDraw)
            if (minFilter != oldSamplerState.minFilter)
                GL.TexParameter(target, TextureParameterName.TextureMinFilter, hasMipmap ? (int)minFilter : (int)minFilterNoMipmap);
#endif

#if !XENKO_PLATFORM_IOS
            if (maxAnisotropy != oldSamplerState.maxAnisotropy && GraphicsDevice.HasAnisotropicFiltering)
                GL.TexParameter(target, (TextureParameterName)OpenTK.Graphics.ES20.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, Description.MaxAnisotropy);
#endif
            if (magFilter != oldSamplerState.magFilter)
                GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)magFilter);
            if (textureWrapS != oldSamplerState.textureWrapS)
                GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)textureWrapS);
            if (textureWrapT != oldSamplerState.textureWrapT)
                GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)textureWrapT);
        }
    }
}

#endif 
