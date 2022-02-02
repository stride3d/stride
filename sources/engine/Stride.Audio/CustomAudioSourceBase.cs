// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Audio
{
    /// <summary>
    /// Simple base class to implement when generating interactive audio. Pre-configured for low latency.
    /// </summary>
    public abstract class CustomAudioSourceBase : ICustomBufferAudioSource
    {
        protected const int BytesPerSample = 2;

        public virtual int BlockSizeInBytes => Channels * 512 * BytesPerSample;

        public virtual int NativeBlockSizeInBytes => 16384;

        public virtual int Channels => 2;

        public virtual int SampleRate => 44100;

        public virtual int Blocks => 4;

        public virtual bool CanSeek => false;

        public virtual void Seek(TimeSpan mediaTime, out bool flushHardwareBuffers)
        {
            flushHardwareBuffers = false;
        }

        public abstract bool ComputeAudioData(AudioData bufferToFill, out bool endOfStream);
    }
}
