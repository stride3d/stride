// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Graphics;

namespace Stride.Video.FFmpeg
{
    /// <summary>
    /// Represents a media, i.e. a context with a collection of streams and associated codecs from a unique source.
    /// </summary>
    /// <seealso cref="FFmpeg.AutoGen.AVFormatContext"/>
    public sealed unsafe class FFmpegMedia : IDisposable
    {
        public static Logger Logger = GlobalLogger.GetLogger(nameof(FFmpegMedia));

        private AVFormatContext* AVFormatContext;
        private SwsContext* pConvertContext;
        private readonly List<FFmpegStream> streams = new List<FFmpegStream>();

        private bool isDisposed;

        private Dictionary<FFmpegStream, StreamInfo> currentStreams = new Dictionary<FFmpegStream, StreamInfo>();

        private bool IsOpen => AVFormatContext != null;

        private AVFrame* pDecodedFrame;
        private AVFrame* pCpuCopyFrame;
        private readonly GraphicsDevice graphicsDevice;

        private AVPixelFormat DestinationPixelFormat => AVPixelFormat.AV_PIX_FMT_RGBA;

        public class StreamInfo
        {
            public FFmpegCodec Codec;
            public VideoImage Image;
            public bool ReachedEnd;
        }

        private enum FrameExtractionStatus
        {
            Succeeded,
            ReachEOF,
            Failed,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegMedia"/> class.
        /// </summary>
        public FFmpegMedia(GraphicsDevice graphicsDevice = null)
        {
            this.graphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// The duration of the media.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// A collection of streams retrieved from this media.
        /// </summary>
        /// <remarks>
        /// The collection is empty until the media is open.
        /// </remarks>
        public IReadOnlyList<FFmpegStream> Streams => streams;

        /// <summary>
        /// The url to the media.
        /// </summary>
        /// <remarks>
        /// It can include a specific protocol such as http or subfile.
        /// </remarks>
        public string Url { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            // Remove all streams
            // Note: native streams are freed along the native context.
            streams.Clear();
            ClearStreamInfo();

            if (!IsOpen || !FFmpegUtils.CheckPlatformSupport())
                return;

            ffmpeg.av_free(pDecodedFrame);
            ffmpeg.av_free(pCpuCopyFrame);
            ffmpeg.sws_freeContext(pConvertContext);

            var pFormatContext = AVFormatContext;
            ffmpeg.avformat_close_input(&pFormatContext);
            ffmpeg.avformat_free_context(pFormatContext);
        }

        [CanBeNull]
        public StreamInfo GetStreamInfo(VideoStream stream) => currentStreams.TryGetValue(stream, out var streamInfo) ? streamInfo : null;

        /// <summary>
        /// Opens this media.
        /// </summary>
        /// <remarks>
        /// Once the media is open, the collection of <see cref="Streams"/> is populated.
        /// </remarks>
        public void Open(string url, long startPosition = 0, long length = -1)
        {
            FFmpegUtils.EnsurePlatformSupport();
            if (isDisposed)
                throw new ObjectDisposedException(nameof(FFmpegMedia));
            if (IsOpen)
                // TODO: log?
                throw new InvalidOperationException(@"Media is already open.");

            if (startPosition != 0 && length != -1)
                url = $@"subfile,,start,{startPosition},end,{startPosition + length},,:{url}";

            var pFormatContext = ffmpeg.avformat_alloc_context();
            var ret = ffmpeg.avformat_open_input(&pFormatContext, url, null, null);
            if (ret < 0)
            {
                Logger.Error($"Could not open file. Error code={ret.ToString("X8")}");
                Logger.Error(GetErrorMessage(ret));
                throw new ApplicationException(@"Could not open file.");
            }

            ret = ffmpeg.avformat_find_stream_info(pFormatContext, null);
            if (ret < 0)
            {
                Logger.Error($"Could not find stream info. Error code={ret.ToString("X8")}");
                Logger.Error(GetErrorMessage(ret));
                throw new ApplicationException(@"Could not find stream info.");
            }

            AVFormatContext = pFormatContext;
            Duration = TimeSpan.FromSeconds((double)AVFormatContext->duration / ffmpeg.AV_TIME_BASE);
            Url = url;

            // Get the streams
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                var stream = FFmpegStream.Create(pFormatContext->streams[i], this);
                streams.Add(stream);
            }

            pDecodedFrame = ffmpeg.av_frame_alloc();
            if (pDecodedFrame == null)
                throw new ApplicationException("Couldn't allocate a frame for decoding.");

            pCpuCopyFrame = ffmpeg.av_frame_alloc();
            if (pCpuCopyFrame == null)
                throw new ApplicationException("Couldn't allocate a frame for hardware decoding.");

            // dispose cached video image from previous file
            ClearStreamInfo();
        }

        public bool SeekToTime([NotNull] FFmpegStream stream, long timestamp)
        {
            FFmpegUtils.EnsurePlatformSupport();

            var skip_to_keyframe = stream.AVStream->skip_to_keyframe;
            try
            {
                if (currentStreams.TryGetValue(stream, out var streamInfo))
                {
                    // flush the codec buffered images
                    streamInfo.Codec.Flush(pDecodedFrame);
                }

                // flush the format buffered images
                ffmpeg.avformat_flush(AVFormatContext);

                // perform the actual seek
                stream.AVStream->skip_to_keyframe = 1;
                var ret = ffmpeg.av_seek_frame(AVFormatContext, stream.Index, timestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
                if (ret < 0)
                {
                    Logger.Error($"Could not seek frame. Error code={ret.ToString("X8")}");
                    Logger.Error(GetErrorMessage(ret));
                    return false;
                }

                return true;
            }
            finally
            {
                stream.AVStream->skip_to_keyframe = skip_to_keyframe;
            }
        }

        [ItemNotNull, NotNull]
        public int ExtractFrames([NotNull] FFmpegStream stream, int count)
        {
            FFmpegUtils.EnsurePlatformSupport();
            if (isDisposed)
                throw new ObjectDisposedException(nameof(FFmpegMedia));
            if (!IsOpen)
                // TODO: log?
                throw new InvalidOperationException(@"Media isn't open.");

            var codecContext = *stream.AVStream->codec;
            var streamInfo = GetStreamInfo(stream);

            var dstData = new byte_ptrArray4();
            var dstLinesize = new int_array4();
            ffmpeg.av_image_fill_arrays(ref dstData, ref dstLinesize, (byte*)streamInfo.Image.Buffer, DestinationPixelFormat, codecContext.width, codecContext.height, 1);
            streamInfo.Image.Linesize = dstLinesize[0];

            var extractedFrameCount = 0;

            var packet = new AVPacket();
            var pPacket = &packet;
            ffmpeg.av_init_packet(pPacket);

            for (int i = 0; i < count; i++)
            {
                var extractionStatus = ExtractNextImage(streamInfo, pPacket, stream.AVStream, dstData, dstLinesize);
                streamInfo.ReachedEnd = extractionStatus == FrameExtractionStatus.ReachEOF;
                if (extractionStatus == FrameExtractionStatus.Succeeded)
                    ++extractedFrameCount;
            }

            return extractedFrameCount;
        }

        /// <summary>
        /// Returns true if the provided stream is a stereoscopic video.
        /// </summary>
        /// <remarks>This function may read the first frame of the video if necessary</remarks>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool IsStereoscopicVideo([NotNull] FFmpegStream stream)
        {
            // try first to get the side data information from the stream if available.
            if (ffmpeg.av_stream_get_side_data(stream.AVStream, AVPacketSideDataType.AV_PKT_DATA_STEREO3D, null) != null)
                return true;

            // Unfortunately the side data was not present in the stream
            // -> we need to decode and look in the first packet and frame.
            var streamInfo = GetStreamInfo(stream);
            var packet = new AVPacket();
            var pPacket = &packet;
            ffmpeg.av_init_packet(pPacket);
            var pCodecContext = streamInfo.Codec.pAVCodecContext;

            try
            {
                while (true)
                {
                    var ret = ffmpeg.av_read_frame(AVFormatContext, pPacket);
                    if (ret < 0)
                    {
                        if (ret == ffmpeg.AVERROR_EOF)
                            return false;

                        Logger.Error($"Could not read frame. Error code={ret.ToString("X8")}.");
                        Logger.Error(GetErrorMessage(ret));
                        return false;
                    }

                    // Note: the other stream might be sound (which we will want to process at some point)
                    if (pPacket->stream_index != stream.AVStream->index)
                        continue;

                    // check the side data on the packet
                    var packetSideData = ffmpeg.av_packet_get_side_data(pPacket, AVPacketSideDataType.AV_PKT_DATA_STEREO3D, null);
                    if (packetSideData != null)
                        return true;

                    ret = ffmpeg.avcodec_send_packet(pCodecContext, pPacket);
                    if (ret < 0)
                    {
                        Logger.Error($"Error while sending packet. Error code={ret.ToString("X8")}");
                        Logger.Error(GetErrorMessage(ret));
                        return false;
                    }

                    ret = ffmpeg.avcodec_receive_frame(pCodecContext, pDecodedFrame);
                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN)) // the media file is not ready yet.
                    {
                        Utilities.Sleep(5);
                        continue;
                    }
                    if (ret < 0)
                    {
                        Logger.Error($"Error while receiving frame. Error code={ret.ToString("X8")}");
                        Logger.Error(GetErrorMessage(ret));
                        return false;
                    }

                    // check the side data on the frame
                    var frameSideData = ffmpeg.av_frame_get_side_data(pDecodedFrame, AVFrameSideDataType.AV_FRAME_DATA_STEREO3D);
                    if (frameSideData != null)
                        return true;

                    // If we reach this point it means that the first packet and frame have been decoded but do not contain any side_data.
                    return false;
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(pPacket);
                ffmpeg.av_frame_unref(pDecodedFrame);

                // return to the beginning of the media file (just in case)
                SeekToTime(stream, 0);
            }
        }

        private StreamInfo GetStreamInfo([NotNull] FFmpegStream stream)
        {
            if (!currentStreams.TryGetValue(stream, out var streamInfo))
            {
                var codecContext = *(stream.AVStream->codec);
                var width = codecContext.width;
                var height = codecContext.height;

                var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(DestinationPixelFormat, width, height, 1);
                convertedFrameBufferSize = (convertedFrameBufferSize + 3) & ~0x03; // align on a boundary of 4 (32-bits)

                currentStreams[stream] = streamInfo = new StreamInfo
                {
                    Codec = new FFmpegCodec(graphicsDevice, &codecContext),
                    Image = new VideoImage(width, height, convertedFrameBufferSize),
                };
            }

            return streamInfo;
        }

        /// <returns>return true if reached end of stream</returns>
        private FrameExtractionStatus ExtractNextImage(StreamInfo streamInfo, AVPacket* pPacket, AVStream* pStream, byte_ptrArray4 dstData, int_array4 dstLinesize)
        {
            AVFrame* pFrame;
            var pCodecContext = streamInfo.Codec.pAVCodecContext;
            var outputImage = streamInfo.Image;

            while (true)
            {
                try
                {
                    var ret = ffmpeg.av_read_frame(AVFormatContext, pPacket);
                    if (ret < 0)
                    {
                        if (ret == ffmpeg.AVERROR_EOF)
                            return FrameExtractionStatus.ReachEOF;

                        Logger.Error($"Could not read frame. Error code={ret.ToString("X8")}.");
                        Logger.Error(GetErrorMessage(ret));
                        return FrameExtractionStatus.Failed;
                    }

                    var packetSideData = ffmpeg.av_packet_get_side_data(pPacket, AVPacketSideDataType.AV_PKT_DATA_STEREO3D, null);

                    // Note: the other stream might be sound (which we will want to process at some point)
                    if (pPacket->stream_index != pStream->index)
                        continue;

                    ret = ffmpeg.avcodec_send_packet(pCodecContext, pPacket);
                    if (ret < 0)
                    {
                        Logger.Error($"Error while sending packet. Error code={ret.ToString("X8")}");
                        Logger.Error(GetErrorMessage(ret));
                        return FrameExtractionStatus.Failed;
                    }

                    ret = ffmpeg.avcodec_receive_frame(pCodecContext, pDecodedFrame);
                    //if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN)) // don't want to block the execution thread
                    //    continue;

                    if (ret < 0)
                    {
                        Logger.Error($"Error while receiving frame. Error code={ret.ToString("X8")}");
                        Logger.Error(GetErrorMessage(ret));
                        // Might be a bad frame, ignore it.
                        return FrameExtractionStatus.Failed;
                    }
                    var frameSideData = ffmpeg.av_frame_get_side_data(pDecodedFrame, AVFrameSideDataType.AV_FRAME_DATA_STEREO3D);

                    // copies the decoded frame on the CPU if needed
                    if (streamInfo.Codec.DecoderOutputTexture == null)
                    {
                        if (pDecodedFrame->format == (int)streamInfo.Codec.HardwarePixelFormat)
                        {
                            // the frame is coming from the GPU
                            ret = ffmpeg.av_hwframe_transfer_data(pCpuCopyFrame, pDecodedFrame, 0);
                            if (ret < 0)
                                throw new ApplicationException("Couldn't transfer frame data from GPU to CPU");

                            pFrame = pCpuCopyFrame;
                        }
                        else
                        {
                            pFrame = pDecodedFrame;
                        }

                        // Create the convert context for frame format convertion
                        var width = pCodecContext->width;
                        var height = pCodecContext->height;
                        var sourcePixFmt = (AVPixelFormat)pFrame->format;
                        pConvertContext = ffmpeg.sws_getCachedContext(pConvertContext, width, height, sourcePixFmt, width, height, DestinationPixelFormat, ffmpeg.SWS_FAST_BILINEAR, null, null, null);
                        if (pConvertContext == null)
                            throw new ApplicationException("Could not initialize the conversion context.");

                        ffmpeg.sws_scale(pConvertContext, pFrame->data, pFrame->linesize, 0, outputImage.Height, dstData, dstLinesize);
                        outputImage.Timestamp = pDecodedFrame->pts;
                    }

                    return FrameExtractionStatus.Succeeded;
                }
                finally
                {
                    ffmpeg.av_packet_unref(pPacket);
                    ffmpeg.av_frame_unref(pDecodedFrame);
                }
            }
        }

        private void ClearStreamInfo()
        {
            foreach (var stream in currentStreams.Values)
            {
                stream.Image.Dispose();
                stream.Codec.Dispose();
            }
            currentStreams.Clear();
        }

        private static string GetErrorMessage(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }
    }
}
#endif
