// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if WINDOWS
using Silk.NET.XAudio;

namespace Stride.Audio;

public sealed partial class AudioBuffer
{
    internal Buffer Buffer;

    public AudioBuffer(int maxBufferSizeBytes)
    {
        Buffer = new();
        unsafe
        {
            var data = stackalloc byte[maxBufferSizeBytes];
            Buffer.PAudioData = data;
        }
    }
}
#endif
