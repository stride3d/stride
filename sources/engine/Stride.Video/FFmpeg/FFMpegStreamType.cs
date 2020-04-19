// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
namespace Stride.Video.FFmpeg
{
    public enum FFMpegStreamType
    {
        Undefined = -1,
        Audio,
        Video,
        Subtitle,
    }
}
#endif
