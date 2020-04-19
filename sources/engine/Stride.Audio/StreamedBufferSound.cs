// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Media;

namespace Stride.Audio
{
    /// <summary>
    /// Sound streamed buffer
    /// </summary>
    /// <remarks>
    /// The sound comes from an external process (such like a video decoder, ...) streaming the audio data into a buffer
    /// </remarks>
    public sealed partial class StreamedBufferSound : SoundBase, IMediaExtractor
    {
        private readonly MediaSynchronizer scheduler;
        private readonly string mediaDataUrl;
        private readonly long startPosition;
        private readonly long length;

        private float speedFactor = 1f;
        
        public StreamedBufferSound(AudioEngine engine, MediaSynchronizer scheduler, string mediaDataUrl, long startPosition, long length, bool spatialized)
        {
            AttachEngine(engine);

            this.scheduler = scheduler;
            this.mediaDataUrl = mediaDataUrl;
            this.startPosition = startPosition;
            this.length = length;

            InitializeImpl();

            NumberOfPackets = 1;
            Spatialized = spatialized;
        }

        partial void InitializeImpl();

        public TimeSpan MediaDuration { get; internal set; }

        public TimeSpan MediaCurrentTime { get; internal set; }

        public MediaType MediaType => MediaType.Audio;

        public float SpeedFactor
        {
            get => speedFactor;
            set
            {
                if (speedFactor == value)
                    return;

                speedFactor = value;
                foreach (SoundInstanceStreamedBuffer instance in Instances)
                    instance.SpeedFactor = speedFactor;
            }
        }

        /// <summary>
        /// Create a new sound effect instance of the sound effect. 
        /// Each instance that can be played and localized independently from others.
        /// </summary>
        /// <returns>A new sound instance</returns>
        /// <exception cref="ObjectDisposedException">The sound has already been disposed</exception>
        public override SoundInstance CreateInstance(AudioListener listener = null, bool useHrtf = false, float directionalFactor = 0.0f, HrtfEnvironment environment = HrtfEnvironment.Small)
        {
            if (listener == null)
                listener = AudioEngine.DefaultListener;

            CheckNotDisposed();

            var newInstance = new SoundInstanceStreamedBuffer(scheduler, this, mediaDataUrl, startPosition, length, listener, useHrtf, directionalFactor, environment)
            {
                Name = Name + " - Instance " + intancesCreationCount,
            };
            RegisterInstance(newInstance);
            
            return newInstance;
        }

        public bool ReachedEndOfMedia()
        {
            foreach (SoundInstanceStreamedBuffer instance in Instances)
            {
                if (!instance.ReachedEndOfMedia())
                    return false;
            }

            return true;
        }

        public void Seek(TimeSpan mediaTime)
        {
            foreach (SoundInstanceStreamedBuffer instance in Instances)
                instance.Seek(mediaTime);
        }

        public bool SeekRequestCompleted()
        {
            foreach (SoundInstanceStreamedBuffer instance in Instances)
            {
                if (!instance.SeekRequestCompleted())
                    return false;
            }

            return true;
        }
    }
}
