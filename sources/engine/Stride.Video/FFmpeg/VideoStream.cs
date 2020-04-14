// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
using System;
using FFmpeg.AutoGen;
using Stride.Core.Annotations;

namespace Stride.Video.FFmpeg
{
    /// <summary>
    /// Represents a video stream from a FFmpeg media.
    /// </summary>
    public sealed unsafe class VideoStream : FFmpegStream
    {
        public VideoStream([NotNull] AVStream* pStream, [NotNull] FFmpegMedia media)
            : base(pStream, media)
        {
            var pCodec = pStream->codec;
            var framerateRatio = pCodec->framerate;

            // HOTFIX (#95)
            if (framerateRatio.den == 0)
            {
                FPS = 0;
                FrameDuration = TimeSpan.Zero;
            }
            else
            {
                FPS = Convert.ToDouble(framerateRatio.num) / Convert.ToDouble(framerateRatio.den);
                FrameDuration = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / Convert.ToInt64(FPS));
            }
            PixelFormat = pCodec->pix_fmt;
            Height = pCodec->height;
            Width = pCodec->width;
        }

        /// <summary>
        /// Video frames per second.
        /// </summary>
        public double FPS { get; }

        /// <summary>
        /// Time interval between two frames.
        /// </summary>
        /// <remarks>
        /// Is it equal to the inverse of <see cref="FPS"/>.
        /// </remarks>
        public TimeSpan FrameDuration { get; }

        /// <summary>
        /// The pixel format of the encoded video.
        /// </summary>
        public AVPixelFormat PixelFormat { get; }

        /// <inheritdoc />
        public override FFMpegStreamType Type => FFMpegStreamType.Video;

        /// <summary>
        /// The height of a frame in the video, in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The width of a frame in the video, in pixels.
        /// </summary>
        public int Width { get; }
    }
}
#endif
