// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_VIDEO_FFMPEG
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Xenko.Core.Annotations;

namespace Xenko.Video.FFmpeg
{
    /// <summary>
    /// Represents a single stream from a FFmpeg media.
    /// </summary>
    /// <seealso cref="global::FFmpeg.AutoGen.AVStream"/>
    public abstract unsafe class FFmpegStream
    {
        /// <summary>
        /// A pointer to the underlying stream.
        /// </summary>
        internal readonly AVStream* AVStream;
        /// <summary>
        /// The media this stream belongs to.
        /// </summary>
        internal readonly FFmpegMedia Media;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegStream"/> class.
        /// </summary>
        protected FFmpegStream([NotNull] AVStream* pStream, FFmpegMedia media)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            AVStream = pStream;
            Codec = pStream->codec->codec_id;
            Media = media;
            Index = pStream->index;
            Metadata = FFmpegUtils.ToDictionary(pStream->metadata);
        }

        /// <summary>
        /// The codec of the media.
        /// </summary>
        public AVCodecID Codec { get; }

        /// <summary>
        /// The duration of the media this stream belongs to.
        /// </summary>
        public TimeSpan Duration => Media.Duration;

        /// <summary>
        /// The index of this stream in the associated format context.
        /// </summary>
        /// <seealso cref="AVFormatContext"/>
        public int Index { get; }

        [NotNull]
        public IReadOnlyDictionary<string, string> Metadata { get; }

        /// <summary>
        /// The type of this stream.
        /// </summary>
        public abstract FFMpegStreamType Type { get; }

        /// <summary>
        /// Creates a new instance of <see cref="FFmpegStream"/> from the provided <paramref name="pStream"/>.
        /// </summary>
        /// <param name="pStream">A pointer to the underlying stream.</param>
        /// <param name="media">The media containing this stream.</param>
        /// <returns></returns>
        /// <remarks>
        /// The actual type of the returned instance depends on the type of stream.
        /// </remarks>
        /// <seealso cref="AudioStream"/>
        /// <seealso cref="SubtitleStream"/>
        /// <seealso cref="VideoStream"/>
        [CanBeNull]
        public static FFmpegStream Create([NotNull] AVStream* pStream, [NotNull] FFmpegMedia media)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (media == null) throw new ArgumentNullException(nameof(media));

            FFmpegStream result;
            var pCodec = pStream->codec;
            switch (pCodec->codec_type)
            {
                case AVMediaType.AVMEDIA_TYPE_AUDIO:
                    result = new AudioStream(pStream, media);
                    break;

                case AVMediaType.AVMEDIA_TYPE_VIDEO:
                    result = new VideoStream(pStream, media);
                    break;

                case AVMediaType.AVMEDIA_TYPE_SUBTITLE:
                    result = new SubtitleStream(pStream, media);
                    break;

                default:
                    return null;
            }
            return result;
        }

        /// <summary>
        /// Converts a timestamp to a real-time value.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public TimeSpan TimestampToTime(long timestamp)
        {
            var timeBase = AVStream->time_base;
            return TimeSpan.FromSeconds((double)timestamp * timeBase.num / timeBase.den);
        }

        /// <summary>
        /// Converts a real-time value to the corresponding timestamp.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public long TimeToTimestamp(TimeSpan time)
        {
            var timeBase = AVStream->time_base;
            return (long)(time.TotalSeconds * timeBase.den / timeBase.num);
        }
    }
}
#endif
