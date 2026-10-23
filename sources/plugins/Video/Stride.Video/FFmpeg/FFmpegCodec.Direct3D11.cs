// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_VIDEO_FFMPEG

using System;
using FFmpeg.AutoGen;

namespace Stride.Video.FFmpeg
{
    // D3D11VA hardware decode wiring. On other graphics APIs the partial methods compile to
    // no-ops and FFmpeg falls back to software decode (default get_format + no hw_device_ctx) —
    // the codec picks a software pix_fmt and sws_scale converts to the Target texture's RGBA.
    public sealed unsafe partial class FFmpegCodec
    {
        private AVBufferRef* pHWDeviceContext;
        private AVCodecContext_get_format getFormat;

        partial void SetupHardwareAcceleration(AVCodecContext* pCodecContext)
        {
            if (Stride.Video.Backends.FFmpegVideoBackendFactory.ForceSoftwareDecode)
            {
                Logger.Info("FFmpegCodec: D3D11VA hwaccel suppressed via FFmpegVideoBackendFactory.ForceSoftwareDecode");
                return;
            }

            // Try to acquire a D3D11VA device first; CI runners (and some retail GPUs) refuse here,
            // in which case we must NOT install our get_format callback — FFmpeg would call it
            // during software decode with only SW pixel formats and our throw would kill the codec.
            AVBufferRef* pHWDeviceContextLocal;
            int hwRet = ffmpeg.av_hwdevice_ctx_create(&pHWDeviceContextLocal, HardwareDeviceType, null, null, 0);
            if (hwRet < 0)
            {
                Logger.Info($"FFmpegCodec: D3D11VA hwaccel unavailable (err={hwRet:X8}), using software decode");
                return;
            }

            IsHardwareAccelerated = true;
            pHWDeviceContext = pHWDeviceContextLocal;
            pCodecContext->hw_device_ctx = ffmpeg.av_buffer_ref(pHWDeviceContext);

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
            Logger.Info("FFmpegCodec: D3D11VA hwaccel enabled");
        }

        // After avcodec_flush_buffers, FFmpeg resets get_format to its default — restoring our
        // callback is required for HW decode to keep picking AV_PIX_FMT_D3D11 (native crash otherwise).
        partial void RestoreGetFormatAfterFlush()
        {
            if (getFormat != null)
                pAVCodecContext->get_format = getFormat;
        }

        partial void DisposeHardwareAcceleration()
        {
            var pHWDeviceContextLocal = pHWDeviceContext;
            ffmpeg.av_buffer_unref(&pHWDeviceContextLocal);
        }
    }
}

#endif
