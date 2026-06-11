// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Stub for platforms with no real audio extractor; suppressed when MediaCodec or AVFoundation is set.
#if !STRIDE_VIDEO_MEDIACODEC && !STRIDE_VIDEO_AVFOUNDATION

using System;

namespace Stride.Audio
{
    public partial class StreamedBufferSoundSource
    {
        private bool ExtractSomeAudioData(out bool endOfFile)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
