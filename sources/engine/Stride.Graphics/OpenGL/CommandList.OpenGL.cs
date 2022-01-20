// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL

using System;
using System.Threading;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride;
using Stride.Shaders;
using Color4 = Stride.Core.Mathematics.Color4;

namespace Stride.Graphics
{
    public partial class CommandList
    {
        // How many frames to wait before allowing non-blocking texture readbacks
        private const int ReadbackFrameDelay = 2;
        private const int MaxBoundRenderTargets = 16;

        internal uint enabledVertexAttribArrays;
        private uint boundProgram = 0;

        internal int BoundStencilReference;
        internal int NewStencilReference;
        internal Color4 BoundBlendFactor;
        internal Color4 NewBlendFactor;

        private bool vboDirty = true;

        private GraphicsDevice.FBOTexture boundDepthStencilBuffer;
        private int boundRenderTargetCount = 0;
        private GraphicsDevice.FBOTexture[] boundRenderTargets = new GraphicsDevice.FBOTexture[MaxBoundRenderTargets];
        internal GraphicsResource[] boundShaderResourceViews = new GraphicsResource[64];
        private GraphicsResource[] shaderResourceViews = new GraphicsResource[64];
        private SamplerState[] samplerStates = new SamplerState[64];

        internal DepthStencilBoundState DepthStencilBoundState;
        internal RasterizerBoundState RasterizerBoundState;

        private Buffer[] constantBuffers = new Buffer[64];

        private uint boundFBO;
        private bool needUpdateFBO = true;

        private PipelineState newPipelineState;
        private PipelineState currentPipelineState;

        private DescriptorSet[] currentDescriptorSets = new DescriptorSet[32];

        internal int activeTexture = 0;

        private IndexBufferView indexBuffer;

        private VertexBufferView[] vertexBuffers = new VertexBufferView[8];

#if !STRIDE_GRAPHICS_API_OPENGLES
        private readonly float[] nativeViewports = new float[4 * MaxViewportAndScissorRectangleCount];
        private readonly int[] nativeScissorRectangles = new int[4 * MaxViewportAndScissorRectangleCount];
#endif

        public static CommandList New(GraphicsDevice device)
        {
            if (device.InternalMainCommandList != null)
            {
                throw new InvalidOperationException("Can't create multiple command lists with OpenGL");
            }
            return new CommandList(device);
        }

        private CommandList(GraphicsDevice device) : base(device)
        {
            device.InternalMainCommandList = this;

            // Default state
            DepthStencilBoundState.DepthBufferWriteEnable = true;
            DepthStencilBoundState.StencilWriteMask = 0xFF;
            RasterizerBoundState.FrontFaceDirection = FrontFaceDirection.Ccw;
#if !STRIDE_GRAPHICS_API_OPENGLES
            RasterizerBoundState.PolygonMode = PolygonMode.Fill;
#endif

            ClearState();
        }

        public void Reset()
        {
        }

        public void Flush()
        {
            
        }

        public CompiledCommandList Close()
        {
            return default(CompiledCommandList);
        }

        public void Clear(Texture depthStencilBuffer, DepthStencilClearOptions options, float depth = 1, byte stencil = 0)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            var clearFBO = GraphicsDevice.FindOrCreateFBO(depthStencilBuffer);
            if (clearFBO != boundFBO)
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, clearFBO);

            ClearBufferMask clearBufferMask =
                ((options & DepthStencilClearOptions.DepthBuffer) == DepthStencilClearOptions.DepthBuffer ? ClearBufferMask.DepthBufferBit : 0)
                | ((options & DepthStencilClearOptions.Stencil) == DepthStencilClearOptions.Stencil ? ClearBufferMask.StencilBufferBit : 0);
            GL.ClearDepth(depth);
            GL.ClearStencil(stencil);

            // Check if we need to change depth mask
            var currentDepthMask = DepthStencilBoundState.DepthBufferWriteEnable;

            if (!currentDepthMask)
                GL.DepthMask(true);
            GL.Clear(clearBufferMask);
            if (!currentDepthMask)
                GL.DepthMask(false);

            if (clearFBO != boundFBO)
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, boundFBO);
        }

        public void Clear(Texture renderTarget, Color4 color)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            var clearFBO = GraphicsDevice.FindOrCreateFBO(renderTarget);
            if (clearFBO != boundFBO)
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, clearFBO);

            // Check if we need to change color mask
            var blendState = currentPipelineState.BlendState;
            var needColorMaskOverride = blendState.ColorWriteChannels != ColorWriteChannels.All;

            if (needColorMaskOverride)
                GL.ColorMask(true, true, true, true);

            GL.ClearColor(color.R, color.G, color.B, color.A);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // revert the color mask value as it was before
            if (needColorMaskOverride)
                blendState.RestoreColorMask(GL);

            if (clearFBO != boundFBO)
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, boundFBO);
        }

        public unsafe void ClearReadWrite(Buffer buffer, Vector4 value)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            if ((buffer.ViewFlags & BufferFlags.UnorderedAccess) != BufferFlags.UnorderedAccess)
                throw new ArgumentException("Buffer does not support unordered access");

            GL.BindBuffer(buffer.BufferTarget, buffer.BufferId);
            GL.ClearBufferData((BufferStorageTarget)buffer.BufferTarget, (SizedInternalFormat)buffer.TextureInternalFormat, buffer.TextureFormat, PixelType.UnsignedInt8888, value);
            GL.BindBuffer(buffer.BufferTarget, 0);
#endif
        }

        public unsafe void ClearReadWrite(Buffer buffer, Int4 value)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            if ((buffer.ViewFlags & BufferFlags.UnorderedAccess) != BufferFlags.UnorderedAccess)
                throw new ArgumentException("Buffer does not support unordered access");

            GL.BindBuffer(buffer.BufferTarget, buffer.BufferId);
            GL.ClearBufferData((BufferStorageTarget)buffer.BufferTarget, (SizedInternalFormat)buffer.TextureInternalFormat, buffer.TextureFormat, PixelType.UnsignedInt8888, value);
            GL.BindBuffer(buffer.BufferTarget, 0);
#endif
        }

        public unsafe void ClearReadWrite(Buffer buffer, UInt4 value)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            if ((buffer.ViewFlags & BufferFlags.UnorderedAccess) != BufferFlags.UnorderedAccess)
                throw new ArgumentException("Buffer does not support unordered access");

            GL.BindBuffer(buffer.BufferTarget, buffer.BufferId);
            GL.ClearBufferData((BufferStorageTarget)buffer.BufferTarget, (SizedInternalFormat)buffer.TextureInternalFormat, buffer.TextureFormat, PixelType.UnsignedInt8888, value);
            GL.BindBuffer(buffer.BufferTarget, 0);
#endif
        }

        public unsafe void ClearReadWrite(Texture texture, Vector4 value)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            if (activeTexture != 0)
            {
                activeTexture = 0;
                GL.ActiveTexture(TextureUnit.Texture0);
            }

            GL.BindTexture(texture.TextureTarget, texture.TextureId);

            GL.ClearTexImage(texture.TextureId, 0, texture.TextureFormat, texture.TextureType, value);

            GL.BindTexture(texture.TextureTarget, 0);
            boundShaderResourceViews[0] = null;
#endif
        }

        public unsafe void ClearReadWrite(Texture texture, Int4 value)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            if (activeTexture != 0)
            {
                activeTexture = 0;
                GL.ActiveTexture(TextureUnit.Texture0);
            }

            GL.BindTexture(texture.TextureTarget, texture.TextureId);

            GL.ClearTexImage(texture.TextureId, 0, texture.TextureFormat, texture.TextureType, value);

            GL.BindTexture(texture.TextureTarget, 0);
            boundShaderResourceViews[0] = null;
#endif
        }

        public unsafe void ClearReadWrite(Texture texture, UInt4 value)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            if (activeTexture != 0)
            {
                activeTexture = 0;
                GL.ActiveTexture(TextureUnit.Texture0);
            }

            GL.BindTexture(texture.TextureTarget, texture.TextureId);

            GL.ClearTexImage(texture.TextureId, 0, texture.TextureFormat, texture.TextureType, value);

            GL.BindTexture(texture.TextureTarget, 0);
            boundShaderResourceViews[0] = null;
#endif
        }

        private void ClearStateImpl()
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            // Clear sampler states
            for (int i = 0; i < samplerStates.Length; ++i)
                samplerStates[i] = null;

            for (int i = 0; i < boundShaderResourceViews.Length; ++i)
            {
                shaderResourceViews[i] = null;
            }

            // Clear active texture state
            activeTexture = 0;
            GL.ActiveTexture(TextureUnit.Texture0);

            // set default states
            currentPipelineState = GraphicsDevice.DefaultPipelineState;
            newPipelineState = GraphicsDevice.DefaultPipelineState;

            // Actually reset states
            //currentPipelineState.BlendState.Apply();
            GL.Disable(EnableCap.Blend);
            GL.ColorMask(true, true, true, true);
            currentPipelineState.DepthStencilState.Apply(this);
            currentPipelineState.RasterizerState.Apply(this);

#if STRIDE_GRAPHICS_API_OPENGLCORE
            GL.Enable(EnableCap.FramebufferSrgb);
#endif
        }

        /// <summary>
        /// Copy a region of a <see cref="GraphicsResource"/> into another.
        /// </summary>
        /// <param name="source">The source from which to copy the data</param>
        /// <param name="regionSource">The region of the source <see cref="GraphicsResource"/> to copy.</param>
        /// <param name="destination">The destination into which to copy the data</param>
        /// <remarks>This might alter some states such as currently bound texture.</remarks>
        public unsafe void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? regionSource, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            var sourceTexture = source as Texture;
            var destTexture = destination as Texture;

            if (sourceTexture == null || destTexture == null)
            {
                throw Internal.Refactor.NewNotImplementedException("Copy is only implemented for Texture objects.");
            }

            // Get parent texture
            if (sourceTexture.ParentTexture != null)
                sourceTexture = sourceTexture.ParentTexture;
            if (destTexture.ParentTexture != null)
                destTexture = sourceTexture.ParentTexture;

            var sourceWidth = Texture.CalculateMipSize(sourceTexture.Description.Width, sourceSubresource % sourceTexture.MipLevels);
            var sourceHeight = Texture.CalculateMipSize(sourceTexture.Description.Height, sourceSubresource % sourceTexture.MipLevels);
            var sourceDepth = Texture.CalculateMipSize(sourceTexture.Description.Depth, sourceSubresource % sourceTexture.MipLevels);

            var sourceRegion = regionSource.HasValue ? regionSource.Value : new ResourceRegion(0, 0, 0, sourceWidth, sourceHeight, sourceDepth);
            var sourceRectangle = new Rectangle(sourceRegion.Left, sourceRegion.Top, sourceRegion.Right - sourceRegion.Left, sourceRegion.Bottom - sourceRegion.Top);

            if (sourceRectangle.Width == 0 || sourceRectangle.Height == 0)
                return;


            if (destTexture.Description.Usage == GraphicsResourceUsage.Staging)
            {
                if (sourceTexture.Description.Usage == GraphicsResourceUsage.Staging)
                {
                    // Staging => Staging
                    if (sourceRegion.Left != 0 || sourceRegion.Top != 0 || sourceRegion.Front != 0
                        || sourceRegion.Right != sourceWidth || sourceRegion.Bottom != sourceHeight || sourceRegion.Back != sourceDepth)
                    {
                        throw new NotSupportedException("ReadPixels from staging texture to staging texture only support full copy of subresource");
                    }

                    GL.BindBuffer(BufferTargetARB.CopyReadBuffer, sourceTexture.PixelBufferObjectId);
                    GL.BindBuffer(BufferTargetARB.CopyWriteBuffer, destTexture.PixelBufferObjectId);
                    GL.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.CopyWriteBuffer,
                        (IntPtr)sourceTexture.ComputeBufferOffset(sourceSubresource, 0),
                        (IntPtr)destTexture.ComputeBufferOffset(destinationSubResource, 0),
                        (UIntPtr)destTexture.ComputeSubresourceSize(destinationSubResource));
                }
                else
                {
                    // GPU => Staging
                    if (dstX != 0 || dstY != 0 || dstZ != 0)
                        throw new NotSupportedException("ReadPixels from staging texture using non-zero destination is not supported");

                    GL.Viewport(0, 0, (uint)sourceWidth, (uint)sourceHeight);

                    var isDepthBuffer = Texture.InternalIsDepthStencilFormat(sourceTexture.Format);

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, isDepthBuffer ? GraphicsDevice.CopyDepthSourceFBO : GraphicsDevice.CopyColorSourceFBO);
                    var attachmentType = FramebufferAttachment.ColorAttachment0;

                    for (int depthSlice = sourceRegion.Front; depthSlice < sourceRegion.Back; ++depthSlice)
                    {
                        attachmentType = GraphicsDevice.UpdateFBO(FramebufferTarget.Framebuffer, new GraphicsDevice.FBOTexture(sourceTexture, sourceSubresource / sourceTexture.MipLevels + depthSlice, sourceSubresource % sourceTexture.MipLevels));

                        GL.BindBuffer(BufferTargetARB.PixelPackBuffer, destTexture.PixelBufferObjectId);
                        GL.ReadPixels(sourceRectangle.Left, sourceRectangle.Top, (uint)sourceRectangle.Width, (uint)sourceRectangle.Height, destTexture.TextureFormat, destTexture.TextureType, (void*)destTexture.ComputeBufferOffset(destinationSubResource, depthSlice));
                        GL.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);

                        destTexture.PixelBufferFrame = GraphicsDevice.FrameCounter;
                    }

                    // Unbind attachment
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType, TextureTarget.Texture2D, 0, 0);

                    // Restore FBO and viewport
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, boundFBO);
                    GL.Viewport((int)viewports[0].X, (int)viewports[0].Y, (uint)viewports[0].Width, (uint)viewports[0].Height);
                }
                return;
            }

            // GPU => GPU
            {
                var isDepthBuffer = Texture.InternalIsDepthStencilFormat(sourceTexture.Format);

                // Use our temporary mutable FBO
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, isDepthBuffer ? GraphicsDevice.CopyDepthSourceFBO : GraphicsDevice.CopyColorSourceFBO);

                var attachmentType = FramebufferAttachment.ColorAttachment0;

                if (activeTexture != 0)
                {
                    activeTexture = 0;
                    GL.ActiveTexture(TextureUnit.Texture0);
                }

                GL.Viewport(0, 0, (uint)sourceWidth, (uint)sourceHeight);

                GL.BindTexture(destTexture.TextureTarget, destTexture.TextureId);

                for (int depthSlice = sourceRegion.Front; depthSlice < sourceRegion.Back; ++depthSlice)
                {
                    // Note: In practice, either it's a 2D texture array and its arrayslice can be non zero, or it's a 3D texture and it's depthslice can be non-zero, but not both at the same time
                    attachmentType = GraphicsDevice.UpdateFBO(FramebufferTarget.Framebuffer, new GraphicsDevice.FBOTexture(sourceTexture, sourceSubresource / sourceTexture.MipLevels + depthSlice, sourceSubresource % sourceTexture.MipLevels));

                    var arraySlice = destinationSubResource / destTexture.MipLevels;
                    var mipLevel = destinationSubResource % destTexture.MipLevels;

                    switch (destTexture.TextureTarget)
                    {
#if !STRIDE_GRAPHICS_API_OPENGLES
                        case TextureTarget.Texture1D:
                            GL.CopyTexSubImage1D(TextureTarget.Texture1D, mipLevel, dstX, sourceRectangle.Left, sourceRectangle.Top, (uint)sourceRectangle.Width);
                            break;
#endif
                        case TextureTarget.Texture2D:
                            GL.CopyTexSubImage2D(TextureTarget.Texture2D, mipLevel, dstX, dstY, sourceRectangle.Left, sourceRectangle.Top, (uint)sourceRectangle.Width, (uint)sourceRectangle.Height);
                            break;
                        case TextureTarget.Texture2DArray:
                            GL.CopyTexSubImage3D(TextureTarget.Texture2DArray, mipLevel, dstX, dstY, arraySlice, sourceRectangle.Left, sourceRectangle.Top, (uint)sourceRectangle.Width, (uint)sourceRectangle.Height);
                            break;
                        case TextureTarget.Texture3D:
                            GL.CopyTexSubImage3D(TextureTarget.Texture3D, mipLevel, dstX, dstY, depthSlice, sourceRectangle.Left, sourceRectangle.Top, (uint)sourceRectangle.Width, (uint)sourceRectangle.Height);
                            break;
                        case TextureTarget.TextureCubeMap:
                            GL.CopyTexSubImage2D(Texture.GetTextureTargetForDataSet2D(destTexture.TextureTarget, arraySlice), mipLevel, dstX, dstY, sourceRectangle.Left, sourceRectangle.Top, (uint)sourceRectangle.Width, (uint)sourceRectangle.Height);
                            break;
                        default:
                            throw new NotSupportedException("Invalid texture target: " + destTexture.TextureTarget);
                    }
                }

                // Unbind texture and force it to be set again next draw call
                GL.BindTexture(destTexture.TextureTarget, 0);
                boundShaderResourceViews[0] = null;

                // Unbind attachment
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType, TextureTarget.Texture2D, 0, 0);

                // Restore FBO and viewport
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, boundFBO);
                GL.Viewport((int)viewports[0].X, (int)viewports[0].Y, (uint)viewports[0].Width, (uint)viewports[0].Height);
            }
        }

        internal unsafe void CopyScaler2D(Texture sourceTexture, Texture destTexture, Rectangle sourceRectangle, Rectangle destRectangle, bool flipY = false)
        {
            // Use rendering
            GL.Viewport(0, 0, (uint)destTexture.Description.Width, (uint)destTexture.Description.Height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GraphicsDevice.FindOrCreateFBO(destTexture));

            var sourceRegionSize = new Vector2(sourceRectangle.Width, sourceRectangle.Height);
            var destRegionSize = new Vector2(destRectangle.Width, destRectangle.Height);

            // Source
            var sourceSize = new Vector2(sourceTexture.Width, sourceTexture.Height);
            var sourceRegionLeftTop = new Vector2(sourceRectangle.Left, sourceRectangle.Top);
            var sourceScale = new Vector2(sourceRegionSize.X / sourceSize.X, sourceRegionSize.Y / sourceSize.Y);
            var sourceOffset = new Vector2(sourceRegionLeftTop.X / sourceSize.X, sourceRegionLeftTop.Y / sourceSize.Y);

            // Dest
            var destSize = new Vector2(destTexture.Width, destTexture.Height);
            var destRegionLeftTop = new Vector2(destRectangle.X, flipY ? destRectangle.Bottom : destRectangle.Y);
            var destScale = new Vector2(destRegionSize.X / destSize.X, destRegionSize.Y / destSize.Y);
            var destOffset = new Vector2(destRegionLeftTop.X / destSize.X, destRegionLeftTop.Y / destSize.Y);

            if (flipY)
                destScale.Y = -destScale.Y;

            var enabledColors = new bool[4];
            GL.GetBoolean(GetPName.ColorWritemask, enabledColors);
            var isDepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            var isCullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);
            var isBlendEnabled = GL.IsEnabled(EnableCap.Blend);
            var isStencilEnabled = GL.IsEnabled(EnableCap.StencilTest);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.StencilTest);
            GL.ColorMask(true, true, true, true);

            // TODO find a better way to detect if sRGB conversion is needed (need to detect if main frame buffer is sRGB or not at init time)
#if STRIDE_GRAPHICS_API_OPENGLES
            // If we are copying from an SRgb texture to a non SRgb texture, we use a special SRGb copy shader
            bool needSRgbConversion = sourceTexture.Description.Format.IsSRgb() && destTexture == GraphicsDevice.WindowProvidedRenderTexture;
#else
            bool needSRgbConversion = false;
#endif
            int offsetLocation, scaleLocation;
            var program = GraphicsDevice.GetCopyProgram(needSRgbConversion, out offsetLocation, out scaleLocation);

            GL.UseProgram(program);

            activeTexture = 0;
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, sourceTexture.TextureId);
            boundShaderResourceViews[0] = null;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            sourceTexture.BoundSamplerState = GraphicsDevice.SamplerStates.PointClamp;

            vboDirty = true;
            enabledVertexAttribArrays |= 1 << 0;
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, GraphicsDevice.GetSquareBuffer().BufferId);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, null);
            GL.Uniform4(offsetLocation, sourceOffset.X, sourceOffset.Y, destOffset.X, destOffset.Y);
            GL.Uniform4(scaleLocation, sourceScale.X, sourceScale.Y, destScale.X, destScale.Y);
            GL.Viewport(0, 0, (uint)destTexture.Width, (uint)destTexture.Height);
            GL.DrawArrays(PrimitiveTypeGl.TriangleStrip, 0, 4);
            GL.UseProgram(boundProgram);

            // Restore context
            if (isDepthTestEnabled)
                GL.Enable(EnableCap.DepthTest);
            if (isCullFaceEnabled)
                GL.Enable(EnableCap.CullFace);
            if (isBlendEnabled)
                GL.Enable(EnableCap.Blend);
            if (isStencilEnabled)
                GL.Enable(EnableCap.StencilTest);
            GL.ColorMask(enabledColors[0], enabledColors[1], enabledColors[2], enabledColors[3]);

            // Restore FBO and viewport
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, boundFBO);
            GL.Viewport((int)viewports[0].X, (int)viewports[0].Y, (uint)viewports[0].Width, (uint)viewports[0].Height);
        }

        internal unsafe void CopyScaler2D(Texture sourceTexture, Rectangle sourceRectangle, Rectangle destRectangle, bool needSRgbConversion = false, bool flipY = false)
        {
            // Use rendering
            GL.Viewport(0, 0, (uint)sourceTexture.Description.Width, (uint)sourceTexture.Description.Height);

            var sourceRegionSize = new Vector2(sourceRectangle.Width, sourceRectangle.Height);
            var destRegionSize = new Vector2(destRectangle.Width, destRectangle.Height);

            // Source
            var sourceSize = new Vector2(sourceTexture.Width, sourceTexture.Height);
            var sourceRegionLeftTop = new Vector2(sourceRectangle.Left, sourceRectangle.Top);
            var sourceScale = new Vector2(sourceRegionSize.X / sourceSize.X, sourceRegionSize.Y / sourceSize.Y);
            var sourceOffset = new Vector2(sourceRegionLeftTop.X / sourceSize.X, sourceRegionLeftTop.Y / sourceSize.Y);

            // Dest
            var destSize = new Vector2(sourceTexture.Width, sourceTexture.Height);
            var destRegionLeftTop = new Vector2(destRectangle.X, flipY ? destRectangle.Bottom : destRectangle.Y);
            var destScale = new Vector2(destRegionSize.X / destSize.X, destRegionSize.Y / destSize.Y);
            var destOffset = new Vector2(destRegionLeftTop.X / destSize.X, destRegionLeftTop.Y / destSize.Y);

            if (flipY)
                destScale.Y = -destScale.Y;

            var enabledColors = new bool[4];
            GL.GetBoolean(GetPName.ColorWritemask, enabledColors);
            var isDepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            var isCullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);
            var isBlendEnabled = GL.IsEnabled(EnableCap.Blend);
            var isStencilEnabled = GL.IsEnabled(EnableCap.StencilTest);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.StencilTest);
            GL.ColorMask(true, true, true, true);

            int offsetLocation, scaleLocation;
            var program = GraphicsDevice.GetCopyProgram(needSRgbConversion, out offsetLocation, out scaleLocation);

            GL.UseProgram(program);

            activeTexture = 0;
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, sourceTexture.TextureId);
            boundShaderResourceViews[0] = null;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            sourceTexture.BoundSamplerState = GraphicsDevice.SamplerStates.PointClamp;

            vboDirty = true;
            enabledVertexAttribArrays |= 1 << 0;
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, GraphicsDevice.GetSquareBuffer().BufferId);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, null);
            GL.Uniform4(offsetLocation, sourceOffset.X, sourceOffset.Y, destOffset.X, destOffset.Y);
            GL.Uniform4(scaleLocation, sourceScale.X, sourceScale.Y, destScale.X, destScale.Y);
            GL.Viewport(0, 0, (uint)sourceTexture.Width, (uint)sourceTexture.Height);
            GL.DrawArrays(PrimitiveTypeGl.TriangleStrip, 0, 4);
            GL.UseProgram(boundProgram);

            // Restore context
            if (isDepthTestEnabled)
                GL.Enable(EnableCap.DepthTest);
            if (isCullFaceEnabled)
                GL.Enable(EnableCap.CullFace);
            if (isBlendEnabled)
                GL.Enable(EnableCap.Blend);
            if (isStencilEnabled)
                GL.Enable(EnableCap.StencilTest);
            GL.ColorMask(enabledColors[0], enabledColors[1], enabledColors[2], enabledColors[3]);

            // Restore viewport
            GL.Viewport((int)viewports[0].X, (int)viewports[0].Y, (uint)viewports[0].Width, (uint)viewports[0].Height);
        }

        /// <summary>
        /// Copy a <see cref="GraphicsResource"/> into another.
        /// </summary>
        /// <param name="source">The source from which to copy the data</param>
        /// <param name="destination">The destination into which to copy the data</param>
        /// <remarks>This might alter some states such as currently bound texture.</remarks>
        public void Copy(GraphicsResource source, GraphicsResource destination)
        {
            // Count subresources
            var subresourceCount = 1;
            var sourceTexture = source as Texture;
            if (sourceTexture != null)
            {
                subresourceCount = sourceTexture.ArraySize * sourceTexture.MipLevels;
            }

            // Copy each subresource
            for (int i = 0; i < subresourceCount; ++i)
            {
                CopyRegion(source, i, null, destination, i);
            }
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            // Check if the source and destination are compatible:
            if (sourceMultisampleTexture.Width != destTexture.Width &&
                sourceMultisampleTexture.Height != destTexture.Height &&
                sourceMultisampleTexture.Format != destTexture.Format)  // TODO: Blitting seems to be okay even if the sizes don't match, but only in case of non-MSAA buffers.
            {
                throw new InvalidOperationException("sourceMultisampleTexture and destTexture don't match in size and format!");
            }

            // Set up the read (source) buffer to use for the blitting operation:
            var readFBOID = GraphicsDevice.FindOrCreateFBO(sourceMultisampleTexture);   // Find the FBO that the sourceMultisampleTexture is bound to.
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFBOID);

            // Set up the draw (destination) buffer to use for the blitting operation:
            var drawFBOID = GraphicsDevice.FindOrCreateFBO(destTexture);   // Find the FBO that the destTexture is bound to.
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFBOID);

            ClearBufferMask clearBufferMask;
            BlitFramebufferFilter blitFramebufferFilter;

            // TODO: PERFORMANCE: We could copy the depth buffer AND color buffer at the same time by doing "ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit".
            if (sourceMultisampleTexture.IsDepthBuffer && destTexture.IsDepthBuffer)
            {
                clearBufferMask = ClearBufferMask.DepthBufferBit;
                blitFramebufferFilter = BlitFramebufferFilter.Nearest;  // Must be set to nearest for depth buffers according to the spec: "GL_INVALID_OPERATION is generated if mask contains any of the GL_DEPTH_BUFFER_BIT or GL_STENCIL_BUFFER_BIT and filter is not GL_NEAREST."
            }
            else
            {
                clearBufferMask = ClearBufferMask.ColorBufferBit;
                blitFramebufferFilter = BlitFramebufferFilter.Linear;   // TODO: STABILITY: For integer formats this has to be set to Nearest.
            }

#if !STRIDE_PLATFORM_IOS
            // MSAA is not supported on iOS currently because OpenTK doesn't expose "GL.BlitFramebuffer()" on iOS for some reason.
            // Do the actual blitting operation:
            GL.BlitFramebuffer(0, 0, sourceMultisampleTexture.Width, sourceMultisampleTexture.Height, 0, 0, destTexture.Width, destTexture.Height, clearBufferMask, blitFramebufferFilter);
#endif
        }

        public void CopyCount(Buffer sourceBuffer, Buffer destBuffer, int offsetToDest)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            Internal.Refactor.ThrowNotImplementedException();
        }

        public void Dispatch(int threadCountX, int threadCountY, int threadCountZ)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if !STRIDE_GRAPHICS_API_OPENGLES
            GL.DispatchCompute((uint)threadCountX, (uint)threadCountY, (uint)threadCountZ);
#else
            Internal.Refactor.ThrowNotImplementedException();
#endif
        }

        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if !STRIDE_GRAPHICS_API_OPENGLES
            GL.BindBuffer(BufferTargetARB.DispatchIndirectBuffer, indirectBuffer.BufferId);

            GL.DispatchComputeIndirect((IntPtr)offsetInBytes);

            GL.BindBuffer(BufferTargetARB.DispatchIndirectBuffer, 0);
#else
            Internal.Refactor.ThrowNotImplementedException();
#endif
        }

        public void Draw(int vertexCount, int startVertex = 0)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            PreDraw();

            GL.DrawArrays(newPipelineState.PrimitiveType, startVertex, (uint)vertexCount);

            GraphicsDevice.FrameTriangleCount += (uint)vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        public void DrawAuto(PrimitiveType primitiveType)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            PreDraw();

            //GL.DrawArraysIndirect(newPipelineState.PrimitiveType, (IntPtr)0);
            //GraphicsDevice.FrameDrawCalls++;
            Internal.Refactor.ThrowNotImplementedException();
        }

        /// <summary>
        /// Draw indexed, non-instanced primitives.
        /// </summary>
        /// <param name="indexCount">Number of indices to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
        public unsafe void DrawIndexed(int indexCount, int startIndexLocation = 0, int baseVertexLocation = 0)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            PreDraw();

#if STRIDE_GRAPHICS_API_OPENGLES
            if (baseVertexLocation != 0)
                throw new NotSupportedException("DrawIndexed with no null baseVertexLocation is not supported on OpenGL ES.");
            GL.DrawElements(newPipelineState.PrimitiveType, (uint)indexCount, indexBuffer.Type, (void*)(indexBuffer.Offset + (startIndexLocation * indexBuffer.ElementSize)));
#else
            GL.DrawElementsBaseVertex(newPipelineState.PrimitiveType, (uint)indexCount, indexBuffer.Type, (void*)(indexBuffer.Offset + (startIndexLocation * indexBuffer.ElementSize)), baseVertexLocation);
#endif

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint)indexCount;
        }

        /// <summary>
        /// Draw indexed, instanced primitives.
        /// </summary>
        /// <param name="indexCountPerInstance">Number of indices read from the index buffer for each instance.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
        public unsafe void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            PreDraw();
#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            GL.DrawElementsInstancedBaseVertex(newPipelineState.PrimitiveType, (uint)indexCountPerInstance, indexBuffer.Type, (void*)(indexBuffer.Offset + (startIndexLocation * indexBuffer.ElementSize)), (uint)instanceCount, baseVertexLocation);
#endif

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint)(indexCountPerInstance * instanceCount);
        }

        /// <summary>
        /// Draw indexed, instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">A buffer containing the GPU generated primitives.</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawIndexedInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {

            if (argumentsBuffer == null) throw new ArgumentNullException(nameof(argumentsBuffer));

#if DEBUG
            //GraphicsDevice.EnsureContextActive();
#endif
            //PreDraw();

            //GraphicsDevice.FrameDrawCalls++;

            Internal.Refactor.ThrowNotImplementedException();
        }

        /// <summary>
        /// Draw non-indexed, instanced primitives.
        /// </summary>
        /// <param name="vertexCountPerInstance">Number of vertices to draw.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
        public void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertexLocation = 0, int startInstanceLocation = 0)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            PreDraw();

            GL.DrawArraysInstanced(newPipelineState.PrimitiveType, startVertexLocation, (uint)vertexCountPerInstance, (uint)instanceCount);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint)(vertexCountPerInstance * instanceCount);
        }

        /// <summary>
        /// Draw instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">An arguments buffer</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            if (argumentsBuffer == null)
                throw new ArgumentNullException(nameof(argumentsBuffer));

#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            PreDraw();

#if STRIDE_GRAPHICS_API_OPENGLES
            GraphicsDevice.FrameDrawCalls++;
            Internal.Refactor.ThrowNotImplementedException();
#else
            GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, argumentsBuffer.BufferId);

            GL.DrawArraysIndirect(newPipelineState.PrimitiveType, (IntPtr)alignedByteOffsetForArgs);

            GL.BindBuffer(BufferTargetARB.DrawIndirectBuffer, 0);

            GraphicsDevice.FrameDrawCalls++;
#endif
        }

        public void BeginProfile(Color4 profileColor, string name)
        {
            if (GraphicsDevice.ProfileEnabled)
            {
                GL.PushDebugGroup(DebugSource.DebugSourceApplication, 1, uint.MaxValue, name);
            }
        }

        public void EndProfile()
        {
            if (GraphicsDevice.ProfileEnabled)
            {
                GL.PopDebugGroup();
            }
        }

        /// <summary>
        /// Submit a timestamp query.
        /// </summary>
        /// <param name="queryPool">The QueryPool owning the query.</param>
        /// <param name="index">The query index.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
#if STRIDE_GRAPHICS_API_OPENGLES
            GraphicsDevice.GLExtDisjointTimerQuery.QueryCounter(queryPool.NativeQueries[index], QueryCounterTarget.Timestamp);
#else
            GL.QueryCounter(queryPool.NativeQueries[index], QueryCounterTarget.Timestamp);
#endif
        }

        public void ResetQueryPool(QueryPool queryPool)
        {
        }

        public unsafe MappedResource MapSubresource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            // This resource has just been recycled by the GraphicsResourceAllocator, we force a rename to avoid GPU=>GPU sync point
            if (resource.DiscardNextMap && mapMode == MapMode.WriteNoOverwrite)
                mapMode = MapMode.WriteDiscard;


            var buffer = resource as Buffer;
            if (buffer != null)
            {
                if (lengthInBytes == 0)
                    lengthInBytes = buffer.Description.SizeInBytes;

                IntPtr mapResult = IntPtr.Zero;

                GL.BindBuffer(buffer.BufferTarget, buffer.BufferId);

#if !STRIDE_GRAPHICS_API_OPENGLES
                //if (mapMode != MapMode.WriteDiscard && mapMode != MapMode.WriteNoOverwrite)
                //    mapResult = GL.MapBuffer(buffer.bufferTarget, mapMode.ToOpenGL());
                //else
#endif
                {
                    // Orphan the buffer (let driver knows we don't need it anymore)
                    if (mapMode == MapMode.WriteDiscard)
                    {
                        doNotWait = true;
                        GL.BufferData(buffer.BufferTarget, (UIntPtr)buffer.Description.SizeInBytes, IntPtr.Zero, buffer.BufferUsageHint);
                    }

                    var unsynchronized = doNotWait && mapMode != MapMode.Read && mapMode != MapMode.ReadWrite;

                    mapResult = (IntPtr)GL.MapBufferRange(buffer.BufferTarget, (IntPtr)offsetInBytes, (UIntPtr)lengthInBytes, mapMode.ToOpenGLMask() | (unsynchronized ? MapBufferAccessMask.MapUnsynchronizedBit : 0));
                }

                return new MappedResource(resource, subResourceIndex, new DataBox { DataPointer = mapResult, SlicePitch = 0, RowPitch = 0 });
            }

            var texture = resource as Texture;
            if (texture != null)
            {
                if (lengthInBytes == 0)
                    lengthInBytes = texture.ComputeSubresourceSize(subResourceIndex);

                if (mapMode == MapMode.Read)
                {
                    if (texture.Description.Usage != GraphicsResourceUsage.Staging)
                        throw new NotSupportedException("Only staging textures can be mapped.");

                    var mipLevel = subResourceIndex % texture.MipLevels;

                    if (doNotWait)
                    {
                        // Wait at least 2 frames after last operation
                        if (GraphicsDevice.FrameCounter < texture.PixelBufferFrame + ReadbackFrameDelay)
                        {
                            return new MappedResource(resource, subResourceIndex, new DataBox(), offsetInBytes, lengthInBytes);
                        }
                    }

                    return MapTexture(texture, true, BufferTargetARB.PixelPackBuffer, texture.PixelBufferObjectId, subResourceIndex, mapMode, offsetInBytes, lengthInBytes);
                }
                else if (mapMode == MapMode.WriteDiscard)
                {
                    if (texture.Description.Usage != GraphicsResourceUsage.Dynamic)
                        throw new NotSupportedException("Only dynamic texture can be mapped.");

                    // Create a temporary unpack pixel buffer
                    // TODO: Pool/allocator? (it's an upload buffer basically)
                    var pixelBufferObjectId = texture.GeneratePixelBufferObject(BufferTargetARB.PixelUnpackBuffer, PixelStoreParameter.UnpackAlignment, BufferUsageARB.DynamicCopy, texture.ComputeSubresourceSize(subResourceIndex));

                    return MapTexture(texture, false, BufferTargetARB.PixelUnpackBuffer, pixelBufferObjectId, subResourceIndex, mapMode, offsetInBytes, lengthInBytes);
                }
            }

            throw Internal.Refactor.NewNotImplementedException("MapSubresource not implemented for type " + resource.GetType());
        }

        public void UnmapSubresource(MappedResource unmapped)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            var texture = unmapped.Resource as Texture;
            if (texture != null)
            {
                if (texture.Description.Usage == GraphicsResourceUsage.Staging)
                {
                    GL.BindBuffer(BufferTargetARB.PixelPackBuffer, texture.PixelBufferObjectId);
                    GL.UnmapBuffer(BufferTargetARB.PixelPackBuffer);
                    GL.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);
                }
                else if (texture.Description.Usage == GraphicsResourceUsage.Dynamic)
                {
                    GL.BindBuffer(BufferTargetARB.PixelUnpackBuffer, unmapped.PixelBufferObjectId);
                    GL.UnmapBuffer(BufferTargetARB.PixelUnpackBuffer);

                    if (activeTexture != 0)
                    {
                        activeTexture = 0;
                        GL.ActiveTexture(TextureUnit.Texture0);
                    }

                    GL.BindTexture(texture.TextureTarget, texture.TextureId);

                    var mipLevel = unmapped.SubResourceIndex % texture.MipLevels;
                    var arraySlice = unmapped.SubResourceIndex / texture.MipLevels;

                    // Bind buffer to texture
                    switch (texture.TextureTarget)
                    {
#if !STRIDE_GRAPHICS_API_OPENGLES
                        case TextureTarget.Texture1D:
                            GL.TexSubImage1D(TextureTarget.Texture1D, mipLevel, 0, (uint)texture.Width, texture.TextureFormat, texture.TextureType, IntPtr.Zero);
                            break;
#endif
                        case TextureTarget.Texture2D:
                            GL.TexSubImage2D(TextureTarget.Texture2D, mipLevel, 0, 0, (uint)texture.Width, (uint)texture.Height, texture.TextureFormat, texture.TextureType, IntPtr.Zero);
                            break;
                        case TextureTarget.Texture2DArray:
                            GL.TexSubImage3D(TextureTarget.Texture2DArray, mipLevel, 0, 0, arraySlice, (uint)texture.Width, (uint)texture.Height, 1, texture.TextureFormat, texture.TextureType, IntPtr.Zero);
                            break;
                        case TextureTarget.Texture3D:
                            GL.TexSubImage3D(TextureTarget.Texture3D, mipLevel, 0, 0, 0, (uint)texture.Width, (uint)texture.Height, (uint)texture.Depth, texture.TextureFormat, texture.TextureType, IntPtr.Zero);
                            break;
                        case TextureTarget.TextureCubeMap:
                            GL.TexSubImage2D(Texture.GetTextureTargetForDataSet2D(texture.TextureTarget, arraySlice), mipLevel, 0, 0, (uint)texture.Width, (uint)texture.Height, texture.TextureFormat, texture.TextureType, IntPtr.Zero);
                            break;
                        default:
                            throw new NotSupportedException("Invalid texture target: " + texture.TextureTarget);
                    }
                    GL.BindTexture(texture.TextureTarget, 0);
                    boundShaderResourceViews[0] = null;
                    GL.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
                    GL.DeleteBuffer(unmapped.PixelBufferObjectId);
                }
                else
                {
                    throw new NotSupportedException("Not supported mapper operation for Usage: " + texture.Description.Usage);
                }
            }
            else
            {
                var buffer = unmapped.Resource as Buffer;
                if (buffer != null)
                {
                    GL.BindBuffer(buffer.BufferTarget, buffer.BufferId);
                    GL.UnmapBuffer(buffer.BufferTarget);
                }
                else // neither texture nor buffer
                {
                    Internal.Refactor.ThrowNotImplementedException("UnmapSubresource not implemented for type " + unmapped.Resource.GetType());
                }
            }
        }

        private unsafe MappedResource MapTexture(Texture texture, bool adjustOffsetForSubresource, BufferTargetARB bufferTarget, uint pixelBufferObjectId, int subResourceIndex, MapMode mapMode, int offsetInBytes, int lengthInBytes)
        {
            int mipLevel = subResourceIndex % texture.MipLevels;

            GL.BindBuffer(bufferTarget, pixelBufferObjectId);
            var mapResult = (IntPtr)GL.MapBufferRange(bufferTarget, (IntPtr)offsetInBytes + (adjustOffsetForSubresource ? texture.ComputeBufferOffset(subResourceIndex, 0) : 0), (UIntPtr)lengthInBytes, mapMode.ToOpenGLMask());
            GL.BindBuffer(bufferTarget, 0);

            return new MappedResource(texture, subResourceIndex, new DataBox { DataPointer = mapResult, SlicePitch = texture.ComputeSlicePitch(mipLevel), RowPitch = texture.ComputeRowPitch(mipLevel) }, offsetInBytes, lengthInBytes)
            {
                PixelBufferObjectId = pixelBufferObjectId,
            };
        }

        internal unsafe void PreDraw()
        {
            // Bind program
            var program = newPipelineState.EffectProgram.ProgramId;
            if (program != boundProgram)
            {
                boundProgram = program;
                GL.UseProgram(boundProgram);
            }

            int vertexBufferSlot = -1;
            var vertexBufferView = default(VertexBufferView);
            Buffer vertexBuffer = null;

            // TODO OPENGL compare newPipelineState.VertexAttribs directly
            if (newPipelineState.VertexAttribs != currentPipelineState.VertexAttribs)
            {
                vboDirty = true;
            }

            // Setup vertex buffers and vertex attributes
            if (vboDirty)
            {
                foreach (var vertexAttrib in newPipelineState.VertexAttribs)
                {
                    if (vertexAttrib.VertexBufferSlot != vertexBufferSlot)
                    {
                        vertexBufferSlot = vertexAttrib.VertexBufferSlot;
                        vertexBufferView = vertexBuffers[vertexBufferSlot];
                        vertexBuffer = vertexBufferView.Buffer;
                        if (vertexBuffer != null)
                        {
                            var vertexBufferResource = vertexBufferView.Buffer.BufferId;
                            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBufferResource);
                        }
                    }

                    var vertexAttribMask = 1U << vertexAttrib.AttributeIndex;

                    // A stride of zero causes automatic stride calculation. To not use the attribute, unbind it in that case
                    if (vertexBuffer == null || vertexBufferView.Stride == 0)
                    {
                        // No VB bound, turn off this attribute
                        if ((enabledVertexAttribArrays & vertexAttribMask) != 0)
                        {
                            enabledVertexAttribArrays &= ~vertexAttribMask;
                            GL.DisableVertexAttribArray((uint)vertexAttrib.AttributeIndex);
                        }
                        continue;
                    }

                    // Enable this attribute if not previously enabled
                    if ((enabledVertexAttribArrays & vertexAttribMask) == 0)
                    {
                        enabledVertexAttribArrays |= vertexAttribMask;
                        GL.EnableVertexAttribArray((uint)vertexAttrib.AttributeIndex);
                    }

                    if (vertexAttrib.IsInteger && !vertexAttrib.Normalized)
                        GL.VertexAttribIPointer((uint)vertexAttrib.AttributeIndex, vertexAttrib.Size, (VertexAttribIType)vertexAttrib.Type, (uint)vertexBufferView.Stride, (void*)(vertexBufferView.Offset + vertexAttrib.Offset));
                    else
                        GL.VertexAttribPointer((uint)vertexAttrib.AttributeIndex, vertexAttrib.Size, vertexAttrib.Type, vertexAttrib.Normalized, (uint)vertexBufferView.Stride, (void*)(vertexBufferView.Offset + vertexAttrib.Offset));
                }

                vboDirty = false;
            }

            // Resources
            newPipelineState.ResourceBinder.BindResources(this, currentDescriptorSets);

            // States
            newPipelineState.Apply(this, currentPipelineState);

            foreach (var textureInfo in newPipelineState.EffectProgram.Textures)
            {
                var boundTexture = boundShaderResourceViews[textureInfo.TextureUnit];
                var shaderResourceView = shaderResourceViews[textureInfo.TextureUnit];
                if (shaderResourceView != null)
                {
                    var texture = shaderResourceView as Texture;
                    var boundSamplerState = texture?.BoundSamplerState ?? GraphicsDevice.DefaultSamplerState;
                    var samplerState = samplerStates[textureInfo.TextureUnit] ?? GraphicsDevice.SamplerStates.LinearClamp;

                    bool textureChanged = shaderResourceView != boundTexture;
                    bool samplerStateChanged = texture != null && samplerState != boundSamplerState;

                    if (textureChanged || samplerStateChanged)
                    {
                        if (activeTexture != textureInfo.TextureUnit)
                        {
                            activeTexture = textureInfo.TextureUnit;
                            GL.ActiveTexture(TextureUnit.Texture0 + textureInfo.TextureUnit);
                        }

                        // Lazy update for texture
                        if (textureChanged)
                        {
                            boundShaderResourceViews[textureInfo.TextureUnit] = shaderResourceView;
                            GL.BindTexture(shaderResourceView.TextureTarget, shaderResourceView.TextureId);
                        }

                        // Lazy update for sampler state
                        if (samplerStateChanged && texture != null)
                        {
                            // TODO: Include hasMipmap in samplerStateChanged
                            bool hasMipmap = texture.Description.MipLevels > 1;

                            samplerState.Apply(hasMipmap, boundSamplerState, texture.TextureTarget);
                            texture.BoundSamplerState = samplerState;
                        }
                    }
                }
            }

            // Update viewports
            SetViewportImpl();

            currentPipelineState = newPipelineState;
        }

        /// <summary>
        /// Sets a constant buffer to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="buffer">The constant buffer to set.</param>
        internal void SetConstantBuffer(ShaderStage stage, int slot, Buffer buffer)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            if (constantBuffers[slot] != buffer)
            {
                // TODO OPENGL if OpenGL ES 2, might be good to have some dirty flags to explain if cbuffer changed
                constantBuffers[slot] = buffer;
                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)slot, buffer != null ? buffer.BufferId : 0);
            }
        }

        private void SetRenderTargetsImpl(Texture depthStencilBuffer, int renderTargetCount, params Texture[] renderTargets)
        {
            if (renderTargetCount > 0)
            {
                // ensure size is coherent
                var expectedWidth = renderTargets[0].Width;
                var expectedHeight = renderTargets[0].Height;
                if (depthStencilBuffer != null)
                {
                    if (expectedWidth != depthStencilBuffer.Width || expectedHeight != depthStencilBuffer.Height)
                        throw new Exception("Depth buffer is not the same size as the render target");
                }
                for (int i = 1; i < renderTargetCount; ++i)
                {
                    if (renderTargets[i] != null && (expectedWidth != renderTargets[i].Width || expectedHeight != renderTargets[i].Height))
                        throw new Exception("Render targets do not have the same size");
                }
            }

#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            boundRenderTargetCount = renderTargetCount;
            for (int i = 0; i < renderTargetCount; ++i)
                boundRenderTargets[i] = renderTargets[i];

            boundDepthStencilBuffer = depthStencilBuffer;

            needUpdateFBO = true;

            SetupTargets();
        }

        private void ResetTargetsImpl()
        {
            boundRenderTargetCount = 0;
        }

        /// <summary>
        /// Sets a sampler state to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="samplerState">The sampler state to set.</param>
        public void SetSamplerState(ShaderStage stage, int slot, SamplerState samplerState)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            samplerStates[slot] = samplerState;
        }

        unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            GL.Scissor(scissorRectangle.Left, scissorRectangle.Top, (uint)scissorRectangle.Width, (uint)scissorRectangle.Height);
        }

        unsafe partial void SetScissorRectanglesImpl(int scissorCount, Rectangle[] scissorRectangles)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            Internal.Refactor.ThrowNotImplementedException();
#else
            for (int i = 0; i < scissorCount; ++i)
            {
                nativeScissorRectangles[4 * i] = scissorRectangles[i].X;
                nativeScissorRectangles[4 * i + 1] = scissorRectangles[i].Y;
                nativeScissorRectangles[4 * i + 2] = scissorRectangles[i].Width;
                nativeScissorRectangles[4 * i + 3] = scissorRectangles[i].Height;
            }

            GL.ScissorArray(0, (uint)scissorCount, nativeScissorRectangles);
#endif
        }

        /// <summary>
        /// Sets a shader resource view to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        internal void SetShaderResourceView(ShaderStage stage, int slot, GraphicsResource shaderResourceView)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            shaderResourceViews[slot] = shaderResourceView;
        }

        /// <inheritdoc/>
        public void SetStreamTargets(params Buffer[] buffers)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            Internal.Refactor.ThrowNotImplementedException();
        }

        /// <summary>
        /// Sets an unordered access view to the shader pipeline.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        /// <param name="uavInitialOffset">The Append/Consume buffer offset. A value of -1 indicates the current offset
        ///     should be kept. Any other values set the hidden counter for that Appendable/Consumable
        ///     UAV. uavInitialCount is only relevant for UAVs which have the 'Append' or 'Counter' buffer
        ///     flag, otherwise the argument is ignored.</param>
        /// <exception cref="System.ArgumentException">Invalid stage.;stage</exception>
        internal void SetUnorderedAccessView(ShaderStage stage, int slot, GraphicsResource unorderedAccessView, int uavInitialOffset)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            if (stage != ShaderStage.Compute)
                throw new ArgumentException("Invalid stage.", nameof(stage));

            Internal.Refactor.ThrowNotImplementedException();
        }
        
        /// <summary>
        /// Unsets an unordered access view from the shader pipeline.
        /// </summary>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        internal void UnsetUnorderedAccessView(GraphicsResource unorderedAccessView)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            
            //Internal.Refactor.ThrowNotImplementedException();
        }

        internal void SetupTargets()
        {
            if (needUpdateFBO)
            {
                boundFBO = GraphicsDevice.FindOrCreateFBO(boundDepthStencilBuffer, boundRenderTargets, boundRenderTargetCount);
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, boundFBO);
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            newPipelineState = pipelineState ?? GraphicsDevice.DefaultPipelineState;
        }

        public void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            var newVertexBuffer = new VertexBufferView(buffer, offset, stride);
            if (vertexBuffers[index] != newVertexBuffer)
            {
                vboDirty = true;
                vertexBuffers[index] = newVertexBuffer;
            }
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            var newIndexBuffer = new IndexBufferView(buffer, offset, is32bits);
            if (indexBuffer != newIndexBuffer)
            {
                // Setup index buffer
                indexBuffer = newIndexBuffer;

                // Setup index buffer
                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer.Buffer != null ? indexBuffer.Buffer.BufferId : 0);
            }
        }

        public void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            // Nothing to do
        }

        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
            for (int i = 0; i < descriptorSets.Length; ++i)
            {
                currentDescriptorSets[index++] = descriptorSets[i];
            }
        }

        public void SetStencilReference(int stencilReference)
        {
            NewStencilReference = stencilReference;
        }

        public void SetBlendFactor(Color4 blendFactor)
        {
            NewBlendFactor = blendFactor;
        }

        private void SetViewportImpl()
        {
            if (!viewportDirty)
                return;

            viewportDirty = false;

#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
            // TODO: Check all non-empty viewports are identical and match what is active in FBO!
            UpdateViewport(viewports[0]);
#else
            UpdateViewports();
#endif
        }

        private void UpdateViewport(Viewport viewport)
        {
            GL.DepthRange(viewport.MinDepth, viewport.MaxDepth);
            GL.Viewport((int)viewport.X, (int)viewport.Y, (uint)viewport.Width, (uint)viewport.Height);
        }

#if !STRIDE_GRAPHICS_API_OPENGLES
        private void UpdateViewports()
        {
            int nbViewports = viewports.Length;
            for (int i = 0; i < boundViewportCount; ++i)
            {
                var currViewport = viewports[i];
                nativeViewports[4 * i] = currViewport.X;
                nativeViewports[4 * i + 1] = currViewport.Y;
                nativeViewports[4 * i + 2] = currViewport.Width;
                nativeViewports[4 * i + 3] = currViewport.Height;
            }
            GL.DepthRange(viewports[0].MinDepth, viewports[0].MaxDepth);
            GL.ViewportArray(0, (uint)boundViewportCount, nativeViewports);
        }
#endif

        public void UnsetReadWriteBuffers()
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
        }

        public void UnsetRenderTargets()
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif

            SetRenderTargets(null, null);
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            var buffer = resource as Buffer;
            if (buffer != null)
            {
                if (!GraphicsDevice.HasTextureBuffers && buffer.BufferId == 0)
                {
                    if (activeTexture != 0)
                    {
                        activeTexture = 0;
                        GL.ActiveTexture(TextureUnit.Texture0);
                    }

                    // On platforms where it's not supported, we use a texture instead of a buffer
                    GL.BindTexture(buffer.TextureTarget, buffer.TextureId);
                    boundShaderResourceViews[0] = null; // bound active texture 0 has changed

                    buffer.UpdateTextureSubresource(databox.DataPointer, 0, 0, buffer.ElementCount);
                }
                else
                {
                    GL.BindBuffer(buffer.BufferTarget, buffer.BufferId);
                    GL.BufferData(buffer.BufferTarget, (uint)buffer.Description.SizeInBytes, (void*)databox.DataPointer, buffer.BufferUsageHint);
                }
            }
            else
            {
                var texture = resource as Texture;
                if (texture != null)
                {
                    if (activeTexture != 0)
                    {
                        activeTexture = 0;
                        GL.ActiveTexture(TextureUnit.Texture0);
                    }

                    // TODO: Handle pitchs
                    // TODO: handle other texture formats
                    GL.BindTexture(texture.TextureTarget, texture.TextureId);
                    boundShaderResourceViews[0] = null; // bound active texture 0 has changed

                    var desc = texture.Description;
                    var mipLevel = subResourceIndex % texture.MipLevels;
                    var arraySlice = subResourceIndex / texture.MipLevels;
                    switch (texture.TextureTarget)
                    {
#if !STRIDE_GRAPHICS_API_OPENGLES
                        case TextureTarget.Texture1D:
                            GL.TexSubImage1D(TextureTarget.Texture1D, mipLevel, 0, (uint)desc.Width, texture.TextureFormat, texture.TextureType, (void*)databox.DataPointer);
                            break;
#endif
                        case TextureTarget.Texture2D:
                            GL.TexSubImage2D(TextureTarget.Texture2D, mipLevel, 0, 0, (uint)desc.Width, (uint)desc.Height, texture.TextureFormat, texture.TextureType, (void*)databox.DataPointer);
                            break;
                        case TextureTarget.Texture2DArray:
                            GL.TexSubImage3D(TextureTarget.Texture2DArray, mipLevel, 0, 0, arraySlice, (uint)desc.Width, (uint)desc.Height, 1, texture.TextureFormat, texture.TextureType, (void*)databox.DataPointer);
                            break;
                        case TextureTarget.Texture3D:
                            GL.TexSubImage3D(TextureTarget.Texture3D, mipLevel, 0, 0, 0, (uint)desc.Width, (uint)desc.Height, (uint)desc.Depth, texture.TextureFormat, texture.TextureType, (void*)databox.DataPointer);
                            break;
                        case TextureTarget.TextureCubeMap:
                            GL.TexSubImage2D(Texture.GetTextureTargetForDataSet2D(texture.TextureTarget, arraySlice), mipLevel, 0, 0, (uint)desc.Width, (uint)desc.Height, texture.TextureFormat, texture.TextureType, (void*)databox.DataPointer);
                            break;
                        default:
                            Internal.Refactor.ThrowNotImplementedException("UpdateSubresource not implemented for texture target " + texture.TextureTarget);
                            break;
                    }
                }
                else // neither texture nor buffer
                {
                    Internal.Refactor.ThrowNotImplementedException("UpdateSubresource not implemented for type " + resource.GetType());
                }
            }
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox, ResourceRegion region)
        {
#if DEBUG
            GraphicsDevice.EnsureContextActive();
#endif
            var texture = resource as Texture;

            if (texture != null)
            {
                var width = region.Right - region.Left;
                var height = region.Bottom - region.Top;
                var depth = region.Back - region.Front;

                var expectedRowPitch = width * texture.TexturePixelSize;

                // determine the opengl read Unpack Alignment
                var packAlignment = 0;
                if ((databox.RowPitch & 1) != 0)
                {
                    if (databox.RowPitch == expectedRowPitch)
                        packAlignment = 1;
                }
                else if ((databox.RowPitch & 2) != 0)
                {
                    var diff = databox.RowPitch - expectedRowPitch;
                    if (diff >= 0 && diff < 2)
                        packAlignment = 2;
                }
                else if ((databox.RowPitch & 4) != 0)
                {
                    var diff = databox.RowPitch - expectedRowPitch;
                    if (diff >= 0 && diff < 4)
                        packAlignment = 4;
                }
                else if ((databox.RowPitch & 8) != 0)
                {
                    var diff = databox.RowPitch - expectedRowPitch;
                    if (diff >= 0 && diff < 8)
                        packAlignment = 8;
                }
                else if (databox.RowPitch == expectedRowPitch)
                {
                    packAlignment = 4;
                }
                if (packAlignment == 0)
                    Internal.Refactor.ThrowNotImplementedException("The data box RowPitch is not compatible with the region width. This requires additional copy to be implemented.");

                // change the Unpack Alignment
                int previousPackAlignment;
                GL.GetInteger(GetPName.UnpackAlignment, out previousPackAlignment);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, packAlignment);

                if (activeTexture != 0)
                {
                    activeTexture = 0;
                    GL.ActiveTexture(TextureUnit.Texture0);
                }

                // Update the texture region
                GL.BindTexture(texture.TextureTarget, texture.TextureId);
                if (texture.Dimension == TextureDimension.Texture3D)
                    GL.TexSubImage3D(texture.TextureTarget, subResourceIndex, region.Left, region.Top, region.Front, (uint)width, (uint)height, (uint)depth, texture.TextureFormat, texture.TextureType, (void*)databox.DataPointer);
                else
                    GL.TexSubImage2D(texture.TextureTarget, subResourceIndex, region.Left, region.Top, (uint)width, (uint)height, texture.TextureFormat, texture.TextureType, (void*)databox.DataPointer);
                boundShaderResourceViews[0] = null; // bound active texture 0 has changed

                // reset the Unpack Alignment
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, previousPackAlignment);
            }
            else
            {
                var buffer = resource as Buffer;
                if (buffer != null)
                {
                    if (!GraphicsDevice.HasTextureBuffers && buffer.BufferId == 0)
                    {
                        if (activeTexture != 0)
                        {
                            activeTexture = 0;
                            GL.ActiveTexture(TextureUnit.Texture0);
                        }

                        // On platforms where it's not supported, we use a texture instead of a buffer
                        GL.BindTexture(buffer.TextureTarget, buffer.TextureId);
                        boundShaderResourceViews[0] = null; // bound active texture 0 has changed

                        buffer.UpdateTextureSubresource(databox.DataPointer, 0, region.Left, region.Right - region.Left);
                    }
                    else
                    {
                        GL.BindBuffer(buffer.BufferTarget, buffer.BufferId);
                        if (region.Left == 0 && region.Right == buffer.SizeInBytes)
                            GL.BufferData(buffer.BufferTarget, (UIntPtr)region.Right, (void*)databox.DataPointer, buffer.BufferUsageHint);
                        else
                            GL.BufferSubData(buffer.BufferTarget, (IntPtr)region.Left, (UIntPtr)(region.Right - region.Left), (void*)databox.DataPointer);
                        GL.BindBuffer(buffer.BufferTarget, 0);
                    }
                }
            }
        }

        struct VertexBufferView
        {
            public readonly Buffer Buffer;
            public readonly int Offset;
            public readonly int Stride;

            public VertexBufferView(Buffer buffer, int offset, int stride)
            {
                Buffer = buffer;
                Offset = offset;
                Stride = stride;
            }

            public static bool operator ==(VertexBufferView left, VertexBufferView right)
            {
                return Equals(left.Buffer, right.Buffer) && left.Offset == right.Offset && left.Stride == right.Stride;
            }

            public static bool operator !=(VertexBufferView left, VertexBufferView right)
            {
                return !(left == right);
            }

            public override bool Equals(object other)
            {
                if (other is VertexBufferView)
                {
                    VertexBufferView p = (VertexBufferView) other;
                    return Equals(Buffer, p.Buffer) && Offset == p.Offset && Stride == p.Stride;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                int result = Buffer.GetHashCode();
                result = (result * 397) ^ Offset;
                result = (result * 397) ^ Stride;
                return result;
            }
        }

        struct IndexBufferView
        {
            public readonly Buffer Buffer;
            public readonly int Offset;
            public readonly DrawElementsType Type;
            public readonly int ElementSize;

            public IndexBufferView(Buffer buffer, int offset, bool is32Bits)
            {
                Buffer = buffer;
                Offset = offset;
                Type = is32Bits ? DrawElementsType.UnsignedInt : DrawElementsType.UnsignedShort;
                ElementSize = is32Bits ? 4 : 2;
            }

            public static bool operator ==(IndexBufferView left, IndexBufferView right)
            {
                return Equals(left.Buffer, right.Buffer) && left.Offset == right.Offset && left.Type == right.Type && left.ElementSize == right.ElementSize;
            }

            public static bool operator !=(IndexBufferView left, IndexBufferView right)
            {
                return !(left == right);
            }

            public override bool Equals(object other)
            {
                if (other is IndexBufferView)
                {
                    IndexBufferView p = (IndexBufferView)other;
                    return Equals(Buffer, p.Buffer) && Offset == p.Offset && Type == p.Type && ElementSize == p.ElementSize;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                int result = Buffer.GetHashCode();
                result = (result * 397) ^ Offset;
                result = (result * 397) ^ Type.GetHashCode();
                result = (result * 397) ^ ElementSize;
                return result;
            }
        }
    }
}

#endif
