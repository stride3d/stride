// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_VIDEO_FFMPEG
using FFmpeg.AutoGen;
using Xenko.Core.Annotations;

namespace Xenko.Video.FFmpeg
{
    /// <summary>
    /// Represents a subtitle stream from a FFmpeg media.
    /// </summary>
    public sealed unsafe class SubtitleStream : FFmpegStream
    {
        public SubtitleStream([NotNull] AVStream* pStream, [NotNull] FFmpegMedia media)
            : base(pStream, media)
        {
        }

        /// <inheritdoc />
        public override FFMpegStreamType Type => FFMpegStreamType.Subtitle;
    }
}
#endif
