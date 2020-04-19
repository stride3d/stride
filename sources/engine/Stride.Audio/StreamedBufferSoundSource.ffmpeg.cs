// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if !STRIDE_PLATFORM_ANDROID || !STRIDE_VIDEO_MEDIACODEC

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
