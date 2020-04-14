// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_VIDEO_FFMPEG && !XENKO_GRAPHICS_API_DIRECT3D11

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Video.FFmpeg;

namespace Xenko.Video
{
    public partial class VideoSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            // Initialize ffmpeg
            FFmpegUtils.PreloadLibraries();
            FFmpegUtils.Initialize();
        }
    }
}

#endif
