// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if !XENKO_PLATFORM_ANDROID || !XENKO_VIDEO_MEDIACODEC

using System;

namespace Xenko.Audio
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
