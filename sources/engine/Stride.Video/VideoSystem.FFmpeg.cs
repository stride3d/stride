// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG && !STRIDE_GRAPHICS_API_DIRECT3D11

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Video.FFmpeg;

namespace Stride.Video
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
