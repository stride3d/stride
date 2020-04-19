// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Stride.Core.Diagnostics;
using Stride.Graphics;

#if STRIDE_GRAPHICS_API_DIRECT3D11
using SharpDX.Direct3D11;
#endif

namespace Stride.Video.FFmpeg
{
    /// <summary>
    /// Represents a codec.
    /// </summary>
    /// <seealso cref="global::FFmpeg.AutoGen.AVCodec"/>
    /// <seealso cref="global::FFmpeg.AutoGen.AVCodecContext"/>
    public sealed unsafe class FFmpegCodec : IDisposable
    {
        public static Logger Logger = GlobalLogger.GetLogger(nameof(FFmpegCodec));

        internal AVCodecContext* pAVCodecContext;

        private AVBufferRef* pHWDeviceContext;

        private bool isDisposed = false;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        private VideoDecoder videoHardwareDecoder;
        private VideoDecoderOutputView videoHardwareDecoderView;
#endif

        private PinnedObject<ID3D11VideoContext> videoContextHandle;
        private PinnedObject<ID3D11VideoDecoder> videoDecoderHandle;
        private PinnedObject<D3D11_VIDEO_DECODER_CONFIG> decoderConfigHandle;
        private PinnedObject<ID3D11VideoDecoderOutputView> decoderOuputViewsHandle;

        private class PinnedObject<T>
        {
            private GCHandle handle;
            private GCHandle arrayHandle;

            public IntPtr Pointer
            {
                get
                {
                    if (arrayHandle.IsAllocated)
                        return arrayHandle.AddrOfPinnedObject();

                    return handle.AddrOfPinnedObject();
                }
            }

            public PinnedObject(T referenceObject, bool asArray = false)
            {
                handle = GCHandle.Alloc(referenceObject, GCHandleType.Pinned);

                if (asArray)
                    arrayHandle = GCHandle.Alloc(handle.AddrOfPinnedObject(), GCHandleType.Pinned);
            }

            public void Dispose()
            {
                if (arrayHandle.IsAllocated)
                    handle.Free();

                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        private AVHWDeviceType HardwareDeviceType => AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        private static SharpDX.DXGI.Format DecoderOuputFormat => SharpDX.DXGI.Format.NV12;
#endif

        public bool IsHardwareAccelerated { get; }

        public AVPixelFormat HardwarePixelFormat => FindPixelFormat(HardwareDeviceType);

        public Texture DecoderOutputTexture { get; internal set; }

        private AVCodecContext_get_format getFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegCodec"/> class.
        /// </summary>
        public FFmpegCodec(GraphicsDevice graphcsDevice, AVCodecContext* originalContext)
        {
            var codecId = originalContext->codec_id;
            var pCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (pCodec == null)
                // TODO: log?
                throw new ApplicationException("Unsupported codec.");
            
            int ret;
            var pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            var pCodecParam = ffmpeg.avcodec_parameters_alloc();
            ret = ffmpeg.avcodec_parameters_from_context(pCodecParam, originalContext);
            if (ret < 0)
                // TODO: log?
                throw new ApplicationException($"Could not retrieve codec parameters. Error code={ret.ToString("X8")}");

            // Set the context get_format function
            getFormat = (context, formats) =>
            {
                AVPixelFormat* pixelFormat;

                for (pixelFormat = formats; *pixelFormat != AVPixelFormat.AV_PIX_FMT_NONE; pixelFormat++)
                {
                    if (*pixelFormat == HardwarePixelFormat)
                        return *pixelFormat;
                }

                throw new ApplicationException("Failed to get HW surface format.");
            };
            pCodecContext->get_format = getFormat;

            ret = ffmpeg.avcodec_parameters_to_context(pCodecContext, pCodecParam);
            if (ret < 0)
                // TODO: log?
                throw new ApplicationException($"Could not fill codec parameters. Error code={ret.ToString("X8")}");

            // create the hardware device context.
            AVBufferRef* pHWDeviceContextLocal;
            if (ffmpeg.av_hwdevice_ctx_create(&pHWDeviceContextLocal, HardwareDeviceType, null, null, 0) >= 0)
            {
                IsHardwareAccelerated = true;
                pHWDeviceContext = pHWDeviceContextLocal;
                pCodecContext->hw_device_ctx = ffmpeg.av_buffer_ref(pHWDeviceContext);
            }

            // Setup hardware acceleration context
            //if (IsHardwareAccelerated)
            //    CreateHarwareAccelerationContext(graphcsDevice, pCodecContext, pCodec);

            if ((pCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
                pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_TRUNCATED;

            if (ffmpeg.avcodec_is_open(pCodecContext) == 0)
            {
                ret = ffmpeg.avcodec_open2(pCodecContext, pCodec, null);
                if (ret < 0)
                    // TODO: log?
                    throw new ApplicationException($"Could not open codec. Error code={ret.ToString("X8")}");
            }
            ffmpeg.avcodec_parameters_free(&pCodecParam);

            pAVCodecContext = pCodecContext;
        }

#if STRIDE_GRAPHICS_API_DIRECT3D11
        private void CreateHarwareAccelerationContext(GraphicsDevice graphicsDevice, AVCodecContext* pAVCodecContext, AVCodec* pCodec)
        {
            var videoDevice = graphicsDevice?.NativeDevice?.QueryInterface<VideoDevice1>();
            var videoContext = graphicsDevice?.NativeDeviceContext?.QueryInterface<VideoContext1>();
            if (videoDevice == null || videoContext == null)
                return;
            
            foreach (var profile in FindVideoFormatCompatibleProfiles(videoDevice, *pCodec))
            {
                // Create and configure the video decoder
                var videoDecoderDescription = new VideoDecoderDescription
                {
                    Guid = profile,
                    SampleWidth = pAVCodecContext->width,
                    SampleHeight = pAVCodecContext->height,
                    OutputFormat = DecoderOuputFormat,
                };

                videoDevice.GetVideoDecoderConfigCount(ref videoDecoderDescription, out var configCount);
                for (var i = 0; i < configCount; ++i)
                {
                    // get and check the decoder configuration for the profile
                    videoDevice.GetVideoDecoderConfig(ref videoDecoderDescription, i, out var decoderConfig);
                    //if (check to performe on the config)
                    //    continue;

                    // create the decoder from the configuration
                    videoDevice.CreateVideoDecoder(ref videoDecoderDescription, ref decoderConfig, out videoHardwareDecoder);

                    // create the decoder output view 
                    var videoDecoderOutputViewDescription = new VideoDecoderOutputViewDescription
                    {
                        DecodeProfile = profile,
                        Dimension = VdovDimension.Texture2D,
                        Texture2D = new Texture2DVdov { ArraySlice = 0, },
                    };
                    DecoderOutputTexture = Texture.New2D(graphicsDevice, pAVCodecContext->width, pAVCodecContext->height, (PixelFormat)DecoderOuputFormat, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
                    videoDevice.CreateVideoDecoderOutputView(DecoderOutputTexture.NativeResource, ref videoDecoderOutputViewDescription, out videoHardwareDecoderView);

                    // Create and fill the hardware context
                    var contextd3d11 = ffmpeg.av_d3d11va_alloc_context();
                                                            
                    var iVideoContext = new ID3D11VideoContext { lpVtbl = (ID3D11VideoContextVtbl*)videoContext.NativePointer };
                    videoContextHandle = new PinnedObject<ID3D11VideoContext>(iVideoContext);
                    contextd3d11->video_context = (ID3D11VideoContext*)videoContextHandle.Pointer;
                    
                    var iVideoDecoder = new ID3D11VideoDecoder { lpVtbl = (ID3D11VideoDecoderVtbl*)videoHardwareDecoder.NativePointer };
                    videoDecoderHandle = new PinnedObject<ID3D11VideoDecoder>(iVideoDecoder);
                    contextd3d11->decoder = (ID3D11VideoDecoder*)videoDecoderHandle.Pointer;

                    decoderConfigHandle = new PinnedObject<D3D11_VIDEO_DECODER_CONFIG>(decoderConfig.ToFFmpegDecoderConfig());
                    contextd3d11->cfg = (D3D11_VIDEO_DECODER_CONFIG*)decoderConfigHandle.Pointer;

                    var iVideoOutputView = new ID3D11VideoDecoderOutputView { lpVtbl = (ID3D11VideoDecoderOutputViewVtbl*)videoHardwareDecoderView.NativePointer };
                    decoderOuputViewsHandle = new PinnedObject<ID3D11VideoDecoderOutputView>(iVideoOutputView, true);
                    contextd3d11->surface = (ID3D11VideoDecoderOutputView**)decoderOuputViewsHandle.Pointer;
                    contextd3d11->surface_count = 1;

                    pAVCodecContext->hwaccel_context = contextd3d11;
                }
            }
        }

        private static IEnumerable<Guid> FindVideoFormatCompatibleProfiles(VideoDevice videoDevice, AVCodec codec)
        {
            for (var i = 0; i < videoDevice.VideoDecoderProfileCount; ++i)
            {
                videoDevice.GetVideoDecoderProfile(i, out var profile);

                // TODO Check profile id

                videoDevice.CheckVideoDecoderFormat(profile, DecoderOuputFormat, out var suppported);
                if (suppported)
                    yield return profile;
            }

            yield break;
        }
#endif

        private static AVPixelFormat FindPixelFormat(AVHWDeviceType deviceType)
        {
            if (deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA)
                return AVPixelFormat.AV_PIX_FMT_D3D11;

            throw new NotImplementedException($"Hardware device type '{deviceType}' not supported");
        }

        public void Flush(AVFrame* pFrame)
        {
            var ret = ffmpeg.avcodec_send_packet(pAVCodecContext, null);
            if (ret < 0)
            {
                Logger.Debug($"Couldn't enter codec flushing mode. Error code={ret.ToString("X8")}");
            }
            else
            {
                while (ffmpeg.avcodec_receive_frame(pAVCodecContext, pFrame) >= 0)
                {
                }
            }

            if (pAVCodecContext != null)
            {
                ffmpeg.avcodec_flush_buffers(pAVCodecContext);
                pAVCodecContext->get_format = getFormat; // for some reason this is needed after the flush (native crash otherwise)
            }
        }
        
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            videoContextHandle?.Dispose();
            videoDecoderHandle?.Dispose();
            decoderConfigHandle?.Dispose();
            decoderOuputViewsHandle?.Dispose();

#if STRIDE_GRAPHICS_API_DIRECT3D11
            videoHardwareDecoder?.Dispose();
            videoHardwareDecoderView?.Dispose();
#endif

            DecoderOutputTexture?.Dispose();
            DecoderOutputTexture = null;

            var pHWDeviceContextLocal = pHWDeviceContext;
            ffmpeg.av_buffer_unref(&pHWDeviceContextLocal);

            var pAVCodecContextLocal = pAVCodecContext;
            ffmpeg.avcodec_close(pAVCodecContextLocal);
            ffmpeg.avcodec_free_context(&pAVCodecContextLocal);
        }
    }
}
#endif
