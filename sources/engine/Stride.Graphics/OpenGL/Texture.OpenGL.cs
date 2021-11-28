// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL
using System;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Abstract class for all textures
    /// </summary>
    public unsafe partial class Texture
    {
        private const int TextureRowPitchAlignment = 1;
        private const int TextureSubresourceAlignment = 1;

        internal const TextureFlags TextureFlagsCustomResourceId = (TextureFlags)0x1000;

        internal SamplerState BoundSamplerState;
        internal int PixelBufferFrame;
        internal int TextureTotalSize;
        private uint pixelBufferObjectId;
        private uint stencilId;

        internal int DepthPitch { get; set; }
        internal int RowPitch { get; set; }
        internal bool IsDepthBuffer { get; private set; }   // TODO: Isn't this redundant? This gets set to the same value as IsDepthStencil...
        internal bool HasStencil { get; private set; }
        internal bool IsRenderbuffer { get; private set; }
        
        internal uint PixelBufferObjectId
        {
            get { return pixelBufferObjectId; }
        }

        internal uint StencilId
        {
            get { return stencilId; }
        }

        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            // always true on OpenGL
            return true;
        }

        internal void SwapInternal(Texture other)
        {
            var tmp = DepthPitch;
            DepthPitch = other.DepthPitch;
            other.DepthPitch = tmp;
            //
            tmp = RowPitch;
            RowPitch = other.RowPitch;
            other.RowPitch = tmp;
            //
            var tmp2 = IsDepthBuffer;
            IsDepthBuffer = other.IsDepthBuffer;
            other.IsDepthBuffer = tmp2;
            //
            tmp2 = HasStencil;
            HasStencil = other.HasStencil;
            other.HasStencil = tmp2;
            //
            tmp2 = IsRenderbuffer;
            HasStencil = other.IsRenderbuffer;
            other.IsRenderbuffer = tmp2;
            //
            Utilities.Swap(ref BoundSamplerState, ref other.BoundSamplerState);
            Utilities.Swap(ref PixelBufferFrame, ref other.PixelBufferFrame);
            Utilities.Swap(ref TextureTotalSize, ref other.TextureTotalSize);
            Utilities.Swap(ref pixelBufferObjectId, ref other.pixelBufferObjectId);
            Utilities.Swap(ref stencilId, ref other.stencilId);
            //
            Utilities.Swap(ref DiscardNextMap, ref other.DiscardNextMap);
            Utilities.Swap(ref TextureId, ref other.TextureId);
            Utilities.Swap(ref TextureTarget, ref other.TextureTarget);
            Utilities.Swap(ref TextureInternalFormat, ref other.TextureInternalFormat);
            Utilities.Swap(ref TextureFormat, ref other.TextureFormat);
            Utilities.Swap(ref TextureType, ref other.TextureType);
            Utilities.Swap(ref TexturePixelSize, ref other.TexturePixelSize);
        }

        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        private void OnRecreateImpl()
        {
            // Dependency: wait for underlying texture to be recreated
            if (ParentTexture != null && ParentTexture.LifetimeState != GraphicsResourceLifetimeState.Active)
                return;

            // Render Target / Depth Stencil are considered as "dynamic"
            if ((Usage == GraphicsResourceUsage.Immutable
                    || Usage == GraphicsResourceUsage.Default)
                && !IsRenderTarget && !IsDepthStencil)
                return;

            if (ParentTexture == null && GraphicsDevice != null)
            {
                GraphicsDevice.RegisterTextureMemoryUsage(-SizeInBytes);
            }

            InitializeFromImpl();
        }

#if STRIDE_PLATFORM_ANDROID //&& USE_GLES_EXT_OES_TEXTURE
        //Prototype: experiment creating GlTextureExternalOes texture
        private void InitializeForExternalOESImpl()
        {
            // TODO: We should probably also set the other parameters if possible, because otherwise we end up with a texture whose metadata says it's of 0x0x0 size and has no format.

            if (TextureId == 0)
            {
                GL.GenTextures(1, out TextureId);

                //Android.Opengl.GLES20.GlBindTexture(Android.Opengl.GLES11Ext.GlTextureExternalOes, TextureId);

                //Any "proper" way to do this? (GLES20 could directly accept it, not GLES30 anymore)
                TextureTarget = (TextureTarget)Android.Opengl.GLES11Ext.GlTextureExternalOes;
                GL.BindTexture(TextureTarget, TextureId);
                
                //GL.BindTexture(TextureTarget, 0);
            }
        }
#endif

        private TextureTarget GetTextureTarget(TextureDimension dimension)
        {
            switch (Dimension)
        	{
                case TextureDimension.Texture1D:
#if !STRIDE_GRAPHICS_API_OPENGLES
                        if (ArraySize > 1)
                            throw new PlatformNotSupportedException("Texture1DArray is not implemented under OpenGL");
                        return TextureTarget.Texture1D;
#endif
                case TextureDimension.Texture2D:
                    return ArraySize > 1 ? TextureTarget.Texture2DArray : TextureTarget.Texture2D;
                case TextureDimension.Texture3D:
                    return TextureTarget.Texture3D;
                case TextureDimension.TextureCube:
                    if (ArraySize > 6)
                        throw new PlatformNotSupportedException("TextureCubeArray is not implemented under OpenGL");
                    return TextureTarget.TextureCubeMap;
            }

            throw new ArgumentOutOfRangeException("TextureDimension couldn't be converted to a TextureTarget.");
        }

        private void CopyParentAttributes()
        {
            TextureId = ParentTexture.TextureId;

            TextureInternalFormat = ParentTexture.TextureInternalFormat;
            TextureFormat = ParentTexture.TextureFormat;
            TextureType = ParentTexture.TextureType;
            TextureTarget = ParentTexture.TextureTarget;
            DepthPitch = ParentTexture.DepthPitch;
            RowPitch = ParentTexture.RowPitch;
            IsDepthBuffer = ParentTexture.IsDepthBuffer;
            HasStencil = ParentTexture.HasStencil;
            IsRenderbuffer = ParentTexture.IsRenderbuffer;

            stencilId = ParentTexture.StencilId;
            pixelBufferObjectId = ParentTexture.PixelBufferObjectId;
        }

        private void InitializeFromImpl(DataBox[] dataBoxes = null)
        {
            if (ParentTexture != null)
            {
                CopyParentAttributes();
            }

            if (TextureId == 0)
            {
                TextureTarget = GetTextureTarget(Dimension);

                bool compressed;
                OpenGLConvertExtensions.ConvertPixelFormat(GraphicsDevice, ref textureDescription.Format, out TextureInternalFormat, out TextureFormat, out TextureType, out TexturePixelSize, out compressed);

                DepthPitch = Description.Width * Description.Height * TexturePixelSize;
                RowPitch = Description.Width * TexturePixelSize;

                IsDepthBuffer = ((Description.Flags & TextureFlags.DepthStencil) != 0);
                if (IsDepthBuffer)
                {
                    HasStencil = InternalHasStencil(Format);
                }
                else
                {
                    HasStencil = false;
                }

                if ((Description.Flags & TextureFlagsCustomResourceId) != 0)
                    return;

                using (var openglContext = GraphicsDevice.UseOpenGLCreationContext())
                {
                    TextureTotalSize = ComputeBufferTotalSize();

                    if (Description.Usage == GraphicsResourceUsage.Staging)
                    {
                        InitializeStagingPixelBufferObject(dataBoxes);
                        return; // TODO: This return causes "GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);" not to get entered. Is that okay?
                    }

                    // Depth textures are renderbuffers for now // TODO: PERFORMANCE: Why? I think we should change that so we can sample them directly.
                    // TODO: enable switch  // TODO: What does this comment even mean?

                    IsRenderbuffer = !Description.IsShaderResource;

                    // Force to renderbuffer if MSAA is on because we don't support MSAA textures ATM (and they don't exist on OpenGL ES).
                    if (Description.IsMultisample)
                    {
                        // TODO: Ideally the caller of this method should be aware of this "force to renderbuffer",
                        //       because the caller won't be able to bind it as a texture.
                        IsRenderbuffer = true;
                    }

                    if (IsRenderbuffer)
                    {
                        CreateRenderbuffer();
                        return; // TODO: This return causes "GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);" not to get entered. Is that okay?
                    }

                    GL.GenTextures(1, out TextureId);
                    GL.BindTexture(TextureTarget, TextureId);
                    SetFilterMode();

                    if (Description.MipLevels == 0)
                        throw new NotImplementedException();

                    var setSize = TextureSetSize(TextureTarget);

                    for (var arrayIndex = 0; arrayIndex < Description.ArraySize; ++arrayIndex)
                    {
                        int offsetArray = arrayIndex * Description.MipLevels;

                        for (int mipLevel = 0; mipLevel < Description.MipLevels; ++mipLevel)
                        {
                            DataBox dataBox;
                            Int3 dimensions = new Int3(CalculateMipSize(Description.Width, mipLevel),
                                                       CalculateMipSize(Description.Height, mipLevel),
                                                       CalculateMipSize(Description.Depth, mipLevel));
                            if (dataBoxes != null && mipLevel < dataBoxes.Length)
                            {
                                if (setSize > 1 && !compressed && dataBoxes[mipLevel].RowPitch != dimensions.X * TexturePixelSize)
                                    throw new NotSupportedException("Can't upload texture with pitch in glTexImage2D/3D.");
                                // Might be possible, need to check API better.
                                dataBox = dataBoxes[offsetArray + mipLevel];
                            }
                            else
                        {
                                dataBox = new DataBox();
                        }

                            switch (TextureTarget)
                        {
                                case TextureTarget.Texture1D:
                                    CreateTexture1D(compressed, dimensions.X, mipLevel, dataBox);
                                    break;
                                case TextureTarget.Texture2D:
                                case TextureTarget.TextureCubeMap:
                                    CreateTexture2D(compressed, dimensions.X, dimensions.Y, mipLevel, arrayIndex, dataBox);
                                    break;
                                case TextureTarget.Texture3D:
                                    CreateTexture3D(compressed, dimensions.X, dimensions.Y, dimensions.Z, mipLevel, dataBox);
                                    break;
                                case TextureTarget.Texture2DArray:
                                    CreateTexture2DArray(compressed, dimensions.X, dimensions.Y, mipLevel, arrayIndex, dataBox);
                                    break;
                            }
                        }
                    }

                    GL.BindTexture(TextureTarget, 0);   // This unbinds the texture.
                    if (openglContext.CommandList != null)
                    {
                        // If we messed up with some states of a command list, mark dirty states
                        openglContext.CommandList.boundShaderResourceViews[openglContext.CommandList.activeTexture] = null;
                    }
                }

                GraphicsDevice.RegisterTextureMemoryUsage(SizeInBytes);
            }
        }

        private void CreateRenderbuffer()
        {
            if (Description.IsDepthStencil) // If it is a depth/stencil attachment:
            {
                ConvertDepthFormat(GraphicsDevice, Description.Format, out var depthRenderbufferFormat);

                CreateRenderbuffer(Width, Height, (int)Description.MultisampleCount, depthRenderbufferFormat, out TextureId);

                if (HasStencil)    // If depth and stencil are stored inside the same renderbuffer:
                {
                    stencilId = TextureId;
                }
            }
            else if (Description.IsRenderTarget)    // If it is a color attachment:
            {
                CreateRenderbuffer(Width, Height, (int)Description.MultisampleCount, TextureInternalFormat, out TextureId);
            }
            else
            {
                throw new NotSupportedException("Requested renderbuffer is neither a render target nor a depth/stencil attachment.");
            }

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);    // Unbinds the renderbuffer.
        }

        private void SetFilterMode()
        {
            if (Description.IsDepthStencil || Description.IsRenderTarget)   // Set the filtering mode of depth, stencil and color FBO attachments:
            {
                // Disable filtering on FBO attachments:
                GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);   // TODO: Do we enter this for MSAA buffers too? Is this an issue?
                GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);   // TODO: Why would we force the filter to "nearest"?
                GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                BoundSamplerState = GraphicsDevice.SamplerStates.PointClamp;

                if (HasStencil)
                {
                    // Since we store depth and stencil in a single texture, we assign the depth buffer's texture ID as the stencil texture ID.
                    stencilId = TextureId;
                }
            }
#if STRIDE_GRAPHICS_API_OPENGLES
            else if (Description.MipLevels <= 1)
            {
                GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);   // TODO: Why does this use the nearest filter for minification? Using Linear filtering would result in a smoother appearance for minified textures.
                GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
#endif

            GL.TexParameter(TextureTarget, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureMaxLevel, Description.MipLevels - 1);
        }

        private void CreateRenderbuffer(int width, int height, int multisampleCount, InternalFormat internalFormat, out uint textureID)
        {
            GL.GenRenderbuffers(1, out textureID);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, textureID);

            if (Description.IsMultisample)
            {
#if !STRIDE_PLATFORM_IOS
                // MSAA is not supported on iOS currently because OpenTK doesn't expose "GL.BlitFramebuffer()" on iOS for some reason.
                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)multisampleCount, internalFormat, (uint)width, (uint)height);
#endif
            }
            else
            {
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, internalFormat, (uint)width, (uint)height);
            }
        }

        private void CreateTexture1D(bool compressed, int width, int mipLevel, DataBox dataBox)
        {
            // TODO: STABILITY: Since 1D textures are not supported on OpenGL ES, what should we do in this case? Throw an exception? I mean currently we just silently ignore that case on OpenGL ES.
#if !STRIDE_GRAPHICS_API_OPENGLES
            if (compressed)
            {
                GL.CompressedTexImage1D(TextureTarget, mipLevel, TextureInternalFormat, (uint)width, 0, (uint)dataBox.SlicePitch, (void*)dataBox.DataPointer);
            }
            else
            {
                GL.TexImage1D(TextureTarget, mipLevel, TextureInternalFormat, (uint)width, 0, TextureFormat, TextureType, (void*)dataBox.DataPointer);
            }
#endif
        }

        private void CreateTexture2D(bool compressed, int width, int height, int mipLevel, int arrayIndex, DataBox dataBox)
        {
            if (IsMultisample)
            {
                throw new InvalidOperationException("Currently if multisampling is on, a renderbuffer will be created (not a texture) in any case and this code will not be reached." +
                                                    "Therefore if this place is reached, it means something went wrong. Once multisampling has been implemented for OpenGL textures, you can remove this exception.");

                if (IsRenderbuffer)
                {
                    GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)Description.MultisampleCount, TextureInternalFormat, (uint)width, (uint)height);
                }
                else
                {
#if STRIDE_GRAPHICS_API_OPENGLES
                    throw new NotSupportedException("Multisample textures are not supported on OpenGL ES.");
#else
                    GL.TexImage2DMultisample(TextureTarget.Texture2DMultisample, (uint)Description.MultisampleCount, TextureInternalFormat, (uint)width, (uint)height, false);
#endif
                }
            }
            else
            {
                var dataSetTarget = GetTextureTargetForDataSet2D(TextureTarget, arrayIndex);
                if (compressed)
                {
                    GL.CompressedTexImage2D(dataSetTarget, mipLevel, TextureInternalFormat, (uint)width, (uint)height, 0, (uint)dataBox.SlicePitch, (void*)dataBox.DataPointer);
                }
                else
                {
                    GL.TexImage2D(dataSetTarget, mipLevel, TextureInternalFormat, (uint)width, (uint)height, 0, TextureFormat, TextureType, (void*)dataBox.DataPointer);
                }
            }
        }

        private void CreateTexture3D(bool compressed, int width, int height, int depth, int mipLevel, DataBox dataBox)
        {
            if (compressed)
            {
                GL.CompressedTexImage3D(TextureTarget, mipLevel, TextureInternalFormat, (uint)width, (uint)height, (uint)depth, 0, (uint)dataBox.SlicePitch, (void*)dataBox.DataPointer);
            }
            else
            {
                GL.TexImage3D(TextureTarget, mipLevel, TextureInternalFormat, (uint)width, (uint)height, (uint)depth, 0, TextureFormat, TextureType, (void*)dataBox.DataPointer);
            }
        }

        private void CreateTexture2DArray(bool compressed, int width, int height, int mipLevel, int arrayIndex, DataBox dataBox)
        {
            // We create all array slices at once, but upload them one by one
            if (arrayIndex == 0)
            {
                if (compressed)
                {
                    GL.CompressedTexImage3D(TextureTarget, mipLevel, TextureInternalFormat, (uint)width, (uint)height, (uint)ArraySize, 0, 0, IntPtr.Zero);
                }
                else
                {
                    GL.TexImage3D(TextureTarget, mipLevel, TextureInternalFormat, (uint)width, (uint)height, (uint)ArraySize, 0, TextureFormat, TextureType, IntPtr.Zero);
                }
            }

            if (dataBox.DataPointer != IntPtr.Zero)
            {
                if (compressed)
                {
                    GL.CompressedTexSubImage3D(TextureTarget, mipLevel, 0, 0, arrayIndex, (uint)width, (uint)height, 1, TextureInternalFormat, (uint)dataBox.SlicePitch, (void*)dataBox.DataPointer);
                }
                else
                {
                    GL.TexSubImage3D(TextureTarget, mipLevel, 0, 0, arrayIndex, (uint)width, (uint)height, 1, TextureFormat, TextureType, (void*)dataBox.DataPointer);
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            using (GraphicsDevice.UseOpenGLCreationContext())
            {
                if (TextureId != 0 && ParentTexture == null)
                {
                    if (IsRenderbuffer)
                        GL.DeleteRenderbuffer(TextureId);
                    else
                        GL.DeleteTexture(TextureId);

                    GraphicsDevice.RegisterTextureMemoryUsage(-SizeInBytes);
                }

                if (stencilId != 0)
                    GL.DeleteRenderbuffer(stencilId);

                if (pixelBufferObjectId != 0)
                    GL.DeleteBuffer(pixelBufferObjectId);
            }

            TextureTotalSize = 0;
            TextureId = 0;
            stencilId = 0;
            pixelBufferObjectId = 0;

            base.OnDestroyed();
        }

        private static void ConvertDepthFormat(GraphicsDevice graphicsDevice, PixelFormat requestedFormat, out InternalFormat depthFormat)
        {
            switch (requestedFormat)
            {
                case PixelFormat.D16_UNorm:
                    depthFormat = InternalFormat.DepthComponent16;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    depthFormat = InternalFormat.Depth24Stencil8;
                    break;
                case PixelFormat.D32_Float:
                    depthFormat = InternalFormat.DepthComponent32;
                    break;
                case PixelFormat.D32_Float_S8X24_UInt:
                    depthFormat = InternalFormat.Depth32fStencil8;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static bool InternalHasStencil(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.D32_Float_S8X24_UInt:
                case PixelFormat.R32_Float_X8X24_Typeless:
                case PixelFormat.X32_Typeless_G8X24_UInt:
                case PixelFormat.D24_UNorm_S8_UInt:
                case PixelFormat.R24_UNorm_X8_Typeless:
                case PixelFormat.X24_Typeless_G8_UInt:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool InternalIsDepthStencilFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.D16_UNorm:
                case PixelFormat.D32_Float:
                case PixelFormat.D32_Float_S8X24_UInt:
                case PixelFormat.R32_Float_X8X24_Typeless:
                case PixelFormat.X32_Typeless_G8X24_UInt:
                case PixelFormat.D24_UNorm_S8_UInt:
                case PixelFormat.R24_UNorm_X8_Typeless:
                case PixelFormat.X24_Typeless_G8_UInt:
                    return true;
                default:
                    return false;
            }
        }

        internal static TextureTarget GetTextureTargetForDataSet2D(TextureTarget target, int arrayIndex)
        {
            // TODO: Proxy from ES 3.1?
            if (target == TextureTarget.TextureCubeMap)
                return TextureTarget.TextureCubeMapPositiveX + arrayIndex;
            return target;
        }

        internal static TextureTarget GetTextureTargetForDataSet3D(TextureTarget target)
        {
            return target;
        }

        private static int TextureSetSize(TextureTarget target)
        {
            // TODO: improve that
#if !STRIDE_GRAPHICS_API_OPENGLES
            if (target == TextureTarget.Texture1D)
                return 1;
#endif
            if (target == TextureTarget.Texture3D || target == TextureTarget.Texture2DArray)
                return 3;
            return 2;
        }

        internal void InternalSetSize(int width, int height)
        {
            // Set backbuffer actual size
            textureDescription.Width = width;
            textureDescription.Height = height;
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat format)
        {
            return format;
        }

        private bool IsFlipped()
        {
            return GraphicsDevice.WindowProvidedRenderTexture == this;
        }

        private void InitializeStagingPixelBufferObject(DataBox[] dataBoxes)
        {
            pixelBufferObjectId = GeneratePixelBufferObject(BufferTargetARB.PixelPackBuffer, PixelStoreParameter.PackAlignment, BufferUsageARB.StreamRead, TextureTotalSize);
            UploadInitialData(BufferTargetARB.PixelPackBuffer, dataBoxes);
        }

        private unsafe void UploadInitialData(BufferTargetARB bufferTarget, DataBox[] dataBoxes)
        {
            // Upload initial data
            int offset = 0;
            var bufferData = IntPtr.Zero;

            if (PixelBufferObjectId != 0)
            {
                GL.BindBuffer(bufferTarget, PixelBufferObjectId);
                bufferData = (IntPtr)GL.MapBufferRange(bufferTarget, IntPtr.Zero, (UIntPtr)TextureTotalSize, MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit);
            }

            if (bufferData != IntPtr.Zero)
            {
                for (var arrayIndex = 0; arrayIndex < Description.ArraySize; ++arrayIndex)
                {
                    var offsetArray = arrayIndex * Description.MipLevels;
                    for (int i = 0; i < Description.MipLevels; ++i)
                    {
                        IntPtr data = IntPtr.Zero;
                        var width = CalculateMipSize(Description.Width, i);
                        var height = CalculateMipSize(Description.Height, i);
                        var depth = CalculateMipSize(Description.Depth, i);
                        if (dataBoxes != null && i < dataBoxes.Length)
                        {
                            data = dataBoxes[offsetArray + i].DataPointer;
                        }

                        if (data != IntPtr.Zero)
                        {
                            Utilities.CopyMemory(bufferData + offset, data, width * height * depth * TexturePixelSize);
                        }

                        offset += width*height*TexturePixelSize;
                    }
                }

                if (PixelBufferObjectId != 0)
                {
                    GL.UnmapBuffer(bufferTarget);
                    GL.BindBuffer(bufferTarget, 0);
                }
            }
        }

        internal uint GeneratePixelBufferObject(BufferTargetARB target, PixelStoreParameter alignment, BufferUsageARB bufferUsage, int totalSize)
        {
            uint result;

            GL.GenBuffers(1, out result);
            GL.BindBuffer(target, result);
            if (RowPitch < 4)
                GL.PixelStore(alignment, 1);
            GL.BufferData(target, (UIntPtr)totalSize, IntPtr.Zero, bufferUsage);
            GL.BindBuffer(target, 0);

            return result;
        }
    }
}

#endif
