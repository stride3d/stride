// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Media;

namespace Stride.Audio
{
    /// <summary>
    /// Base class for sound sources that stream audio buffer-wise, i.e. do not hold all audio data in memory.
    /// </summary>
    public abstract class StreamedBufferSoundSourceBase : DynamicSoundSource
    {
        public float SpeedFactor { get; set; } = 1f;
        public int Channels { get; protected set; }
        public int SampleRate { get; protected set; }
        public MediaType MediaType => MediaType.Audio;
        public TimeSpan MediaDuration { get; protected set; }
        public bool IsDisposed => isDisposed;

        public StreamedBufferSoundSourceBase(SoundInstance soundInstance, int numberOfBuffers, int maxBufferSizeBytes)
            : base(soundInstance, numberOfBuffers, maxBufferSizeBytes)
        { }

        /// <summary>
        /// The media scheduler will check this field to determine whether he can stop waiting for the extractors getting ready
        /// </summary>
        protected volatile bool seekRequestCompleted = true;
        public bool SeekRequestCompleted()
        {
            return seekRequestCompleted;
        }

        protected volatile bool isEof;
        public bool ReachedEndOfMedia()
        {
            return isEof;
        }

        public abstract void Seek(TimeSpan mediaTime);
    }
}
