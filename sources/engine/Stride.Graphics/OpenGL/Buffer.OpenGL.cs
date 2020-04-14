// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL 
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Stride.Core;
#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
using PixelFormatGl = OpenTK.Graphics.OpenGL.PixelFormat;
using TextureTarget2d = OpenTK.Graphics.OpenGL.TextureTarget;
#endif

namespace Stride.Graphics
{
    public partial class Buffer
    {
        internal const int BufferTextureEmulatedWidth = 4096;

        internal int BufferId;
        internal BufferTarget BufferTarget;
        internal BufferUsageHint BufferUsageHint;

        private int bufferTextureElementSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected Buffer InitializeFromImpl(BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            bufferDescription = description;
            ViewFlags = viewFlags;

            bool isCompressed;
            OpenGLConvertExtensions.ConvertPixelFormat(GraphicsDevice, ref viewFormat, out TextureInternalFormat, out TextureFormat, out TextureType, out bufferTextureElementSize, out isCompressed);

            ViewFormat = viewFormat;

            Recreate(dataPointer);

            if (GraphicsDevice != null)
            {
                GraphicsDevice.RegisterBufferMemoryUsage(SizeInBytes);
            }

            return this;
        }

        public void Recreate(IntPtr dataPointer)
        {
            if ((ViewFlags & BufferFlags.VertexBuffer) == BufferFlags.VertexBuffer)
            {
                BufferTarget = BufferTarget.ArrayBuffer;
            }
            else if ((ViewFlags & BufferFlags.IndexBuffer) == BufferFlags.IndexBuffer)
            {
                BufferTarget = BufferTarget.ElementArrayBuffer;
            }
            else if ((ViewFlags & BufferFlags.UnorderedAccess) == BufferFlags.UnorderedAccess)
            {
#if STRIDE_GRAPHICS_API_OPENGLES
                throw new NotSupportedException("GLES not support UnorderedAccess buffer");
#else
                BufferTarget = BufferTarget.ShaderStorageBuffer;
#endif
            }
            else if ((ViewFlags & BufferFlags.ShaderResource) == BufferFlags.ShaderResource && GraphicsDevice.HasTextureBuffers)
            {
#if STRIDE_GRAPHICS_API_OPENGLES
                Internal.Refactor.ThrowNotImplementedException();
#else
                BufferTarget = BufferTarget.TextureBuffer;
#endif
            }

            Init(dataPointer);
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            if (Description.Usage == GraphicsResourceUsage.Immutable
                || Description.Usage == GraphicsResourceUsage.Default)
                return false;

            Recreate(IntPtr.Zero);

            return true;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            using (GraphicsDevice.UseOpenGLCreationContext())
            {
                GL.DeleteTextures(1, ref TextureId);
                GL.DeleteBuffers(1, ref BufferId);
            }

            BufferId = 0;

            if (GraphicsDevice != null)
            {
                GraphicsDevice.RegisterBufferMemoryUsage(-SizeInBytes);
            }

            base.OnDestroyed();
        }

        protected void Init(IntPtr dataPointer)
        {
            switch (Description.Usage)
            {
                case GraphicsResourceUsage.Default:
                case GraphicsResourceUsage.Immutable:
                    BufferUsageHint = BufferUsageHint.StaticDraw;
                    break;
                case GraphicsResourceUsage.Dynamic:
                case GraphicsResourceUsage.Staging:
                    BufferUsageHint = BufferUsageHint.DynamicDraw;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("description.Usage");
            }

            using (var openglContext = GraphicsDevice.UseOpenGLCreationContext())
            {
                if ((Flags & BufferFlags.ShaderResource) != 0 && !GraphicsDevice.HasTextureBuffers)
                {
                    // Create a texture instead of a buffer on platforms where it's not supported
                    elementCount = SizeInBytes / bufferTextureElementSize;
                    TextureTarget = TextureTarget.Texture2D;
                    GL.GenTextures(1, out TextureId);
                    GL.BindTexture(TextureTarget, TextureId);

                    GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                    GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                    UpdateTextureSubresource(dataPointer, 0, 0, SizeInBytes);

                    GL.BindTexture(TextureTarget, 0);

                    if (openglContext.CommandList != null)
                    {
                        // If we messed up with some states of a command list, mark dirty states
                        openglContext.CommandList.boundShaderResourceViews[openglContext.CommandList.activeTexture] = null;
                    }
                }
                else
                {
                    // If we're on main context, unbind VAO before binding context.
                    // It will be bound again on next draw.
                    //if (!creationContext.UseDeviceCreationContext)
                    //    GraphicsDevice.UnbindVertexArrayObject();

                    GL.GenBuffers(1, out BufferId);
                    GL.BindBuffer(BufferTarget, BufferId);
                    GL.BufferData(BufferTarget, (IntPtr)Description.SizeInBytes, dataPointer, BufferUsageHint);
                    GL.BindBuffer(BufferTarget, 0);

                    if ((Flags & BufferFlags.ShaderResource) != 0)
                    {
#if STRIDE_GRAPHICS_API_OPENGLES
                        Internal.Refactor.ThrowNotImplementedException();
#else
                        TextureTarget = TextureTarget.TextureBuffer;
                        GL.GenTextures(1, out TextureId);
                        GL.BindTexture(TextureTarget, TextureId);
                        // TODO: Check if this is really valid to cast PixelInternalFormat to SizedInternalFormat in all cases?
                        GL.TexBuffer(TextureBufferTarget.TextureBuffer, (SizedInternalFormat)TextureInternalFormat, BufferId);
#endif
                    }
                }
            }
        }

        internal void UpdateTextureSubresource(IntPtr dataPointer, int subresouceLevel, int offset, int count)
        {
            // If overwriting everything, create a new texture
            if (offset == 0 && count == SizeInBytes)
                GL.TexImage2D((TextureTarget2d)TextureTarget, subresouceLevel, TextureInternalFormat, Math.Min(BufferTextureEmulatedWidth, elementCount), (elementCount + BufferTextureEmulatedWidth - 1) / BufferTextureEmulatedWidth, 0, TextureFormat, TextureType, IntPtr.Zero);

            // Work with full elements
            Debug.Assert(offset % bufferTextureElementSize == 0 && count % bufferTextureElementSize == 0, "When updating a buffer texture, offset and count should be a multiple of the element size");
            offset /= bufferTextureElementSize;
            count /= bufferTextureElementSize;

            // Upload data
            if (dataPointer != IntPtr.Zero)
            {
                // First line
                if (offset % BufferTextureEmulatedWidth != 0)
                {
                    var firstLineSize = Math.Min(count, BufferTextureEmulatedWidth - (offset % BufferTextureEmulatedWidth));

                    GL.TexSubImage2D((TextureTarget2d)TextureTarget, 0,
                        offset % BufferTextureEmulatedWidth, offset / BufferTextureEmulatedWidth, // coordinates
                        BufferTextureEmulatedWidth - (offset % BufferTextureEmulatedWidth), 1, // size
                        TextureFormat, TextureType, dataPointer);

                    offset += firstLineSize;
                    count -= firstLineSize;
                    dataPointer += firstLineSize * bufferTextureElementSize;
                }

                // Middle lines
                if (count / BufferTextureEmulatedWidth > 0)
                    GL.TexSubImage2D((TextureTarget2d)TextureTarget, 0,
                        0, offset / BufferTextureEmulatedWidth, // coordinates
                        BufferTextureEmulatedWidth, count / BufferTextureEmulatedWidth, // size
                        TextureFormat, TextureType, dataPointer);

                // Last line is done separately (to avoid buffer overrun if last line is not multiple of BufferTextureEmulatedWidth)
                if (count % BufferTextureEmulatedWidth != 0)
                    GL.TexSubImage2D((TextureTarget2d)TextureTarget, 0,
                        0, count / BufferTextureEmulatedWidth, // coordinates
                        count % BufferTextureEmulatedWidth, 1, // size
                        TextureFormat, TextureType, dataPointer + (count/ BufferTextureEmulatedWidth * BufferTextureEmulatedWidth) * bufferTextureElementSize);
            }
        }
    }
} 
#endif
