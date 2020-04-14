// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
using FFmpeg.AutoGen;
using Stride.Core.Annotations;

namespace Stride.Video.FFmpeg
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
