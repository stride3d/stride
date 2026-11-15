// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG

using System;
using FFmpeg.AutoGen;
using Stride.Core.Diagnostics;

namespace Stride.Video.FFmpeg
{
    /// <summary>
    /// Represents a codec.
    /// </summary>
    /// <seealso cref="AVCodec"/>
    /// <seealso cref="AVCodecContext"/>
    public sealed unsafe partial class FFmpegCodec : IDisposable
    {
        public static Logger Logger = GlobalLogger.GetLogger(nameof(FFmpegCodec));

        internal AVCodecContext* pAVCodecContext;

        private bool isDisposed = false;

        private AVHWDeviceType HardwareDeviceType => AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA;

        public bool IsHardwareAccelerated { get; private set; }

        public AVPixelFormat HardwarePixelFormat => FindPixelFormat(HardwareDeviceType);

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegCodec"/> class.
        /// </summary>
        public FFmpegCodec(AVCodecParameters* originalCodecpar)
        {
            var codecId = originalCodecpar->codec_id;
            var pCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (pCodec == null)
                // TODO: log?
                throw new ApplicationException("Unsupported codec.");

            int ret;
            var pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);

            ret = ffmpeg.avcodec_parameters_to_context(pCodecContext, originalCodecpar);
            if (ret < 0)
                // TODO: log?
                throw new ApplicationException($"Could not fill codec parameters. Error code={ret.ToString("X8")}");

            SetupHardwareAcceleration(pCodecContext);

            if (ffmpeg.avcodec_is_open(pCodecContext) == 0)
            {
                ret = ffmpeg.avcodec_open2(pCodecContext, pCodec, null);
                if (ret < 0)
                    // TODO: log?
                    throw new ApplicationException($"Could not open codec. Error code={ret.ToString("X8")}");
            }

            pAVCodecContext = pCodecContext;
        }

        /// <summary>Initializes the platform-specific hardware decode context, when supported.</summary>
        partial void SetupHardwareAcceleration(AVCodecContext* pCodecContext);

        /// <summary>Restores the platform-specific get_format callback after <see cref="ffmpeg.avcodec_flush_buffers"/>.</summary>
        partial void RestoreGetFormatAfterFlush();

        /// <summary>Disposes the platform-specific hardware decode resources.</summary>
        partial void DisposeHardwareAcceleration();

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
                RestoreGetFormatAfterFlush();
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            DisposeHardwareAcceleration();

            var pAVCodecContextLocal = pAVCodecContext;
            ffmpeg.avcodec_free_context(&pAVCodecContextLocal);
        }
    }
}

#endif
