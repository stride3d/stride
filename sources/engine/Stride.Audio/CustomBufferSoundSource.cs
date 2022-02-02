// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Media;

namespace Stride.Audio
{
    /// <summary>
    /// Interface to implement to provide custom audio data via callback.
    /// </summary>
    public interface ICustomBufferAudioSource
    {
        /// <summary>
        /// Number of audio channels, currently only 1 (mono, for sounds that can be spatialized) or 2 (stereo, sent directly to audio card) is supported.
        /// </summary>
        int Channels { get; }

        /// <summary>
        /// Sample rate, i.e. 44100 or 48000
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// The size in bytes of each block that gets produced by this source.
        /// i.e. Channels * 512 * BytesPerSample
        /// </summary>
        int BlockSizeInBytes { get; }
        
        /// <summary>
        /// The number of audio blocks to allocate, should be greater than 2, but 4 is recommended.
        /// </summary>
        int Blocks { get; }

        /// <summary>
        /// Buffer size to allocate on the audio driver, 16384 is recommende. Increase if you want to produce bigger blocks. 
        /// </summary>
        int NativeBlockSizeInBytes { get; }

        /// <summary>
        /// Return true if this audio source supports seeking.
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        /// Called on the audio thread when <see cref="CanSeek"/> returns true and the seek is called by the user.
        /// </summary>
        /// <param name="mediaTime">The time to jump to.</param>
        /// <param name="flushHardwareBuffers">Return true if the audio driver should flush the current buffers and start the stream anew.</param>
        void Seek(TimeSpan mediaTime, out bool flushHardwareBuffers);
        
        /// <summary>
        /// The main callback on the audio thread to produce the audio data.
        /// </summary>
        /// <param name="storageBufferToFill">The buffer to fill and set the amount of data that was filled.</param>
        /// <param name="endOfStream">Set this to true to end this sound instance.</param>
        /// <returns>True if any data was produced and should be send to the audio device.</returns>
        bool ComputeAudioData(AudioData storageBufferToFill, out bool endOfStream);
    }

    /// <summary>
    /// The audio is created by an external sound source implemented by the user.
    /// </summary>
    public class CustomBufferSoundSource : StreamedBufferSoundSourceBase
    {
        private int numberOfBuffers;

        private readonly object objLock = new object();

        ICustomBufferAudioSource audioSource;

        private TimeSpan mediaCurrentTimeMax;
        private TimeSpan mediaCurrentTime;
        private TimeSpan commandSeekTime;

        private int sentBuffersCount = 0;
        private int accumulatedSentBytesCount = 0;

        private float byteRatePerSecond; //bytes per second

        /// <summary>
        /// Temporary buffers for accumulating the data we're extracting before sending them to the AudioLayer
        /// </summary>
        private AudioData storageBuffer;

        private bool beginningOfStream;

        private PlayRange playRange;
        private volatile bool looped;

        private DateTime lastLoopTime = DateTime.Now;

        public override int MaxNumberOfBuffers => audioSource.Blocks;

        public CustomBufferSoundSource(SoundInstanceStreamedBuffer instance, ICustomBufferAudioSource customBufferAudioSource)
            : base(instance, customBufferAudioSource.Blocks, customBufferAudioSource.NativeBlockSizeInBytes)
        {
            audioSource = customBufferAudioSource;
            numberOfBuffers = audioSource.Blocks;
            storageBuffer = new AudioData(audioSource.BlockSizeInBytes);
            Channels = audioSource.Channels;
            SampleRate = audioSource.SampleRate;
            NewSources.Add(this);
        }

        protected override bool CanFill => Commands.IsEmpty && base.CanFill;

        /// <summary>
        /// Sets if the stream should be played in loop
        /// </summary>
        /// <param name="loop">if looped or not</param>
        public override void SetLooped(bool loop)
        {
            lock (objLock)
            {
                looped = loop;
            }
        }

        public TimeSpan MediaCurrentTime
        {
            get
            {
                lock (objLock)
                {
                    return mediaCurrentTime;
                }
            }
            set
            {
                lock (objLock)
                {
                    mediaCurrentTime = value;
                }
            }
        }

        public override void Seek(TimeSpan mediaTime)
        {
            seekRequestCompleted = false;
            commandSeekTime = mediaTime;
            Commands.Enqueue(AsyncCommand.Seek);
        }

        protected override void InitializeInternal()
        {
            byteRatePerSecond = SampleRate * (Channels * 2); //2 = bit depth (Pcm16bit)

            base.InitializeInternal();
        }

        protected override void PrepareInternal()
        {
            base.PrepareInternal();

            beginningOfStream = true;
        }

        protected override void UpdateInternal()
        {
            base.UpdateInternal();

            // update the media presentation time
            var currentTime = DateTime.Now;
            var elapsedTime = (currentTime - lastLoopTime);
            if (elapsedTime > TimeSpan.Zero)
            {
                if (SpeedFactor != 1)
                    elapsedTime = TimeSpan.FromMilliseconds(elapsedTime.TotalMilliseconds * SpeedFactor);

                var mediaCurrentTime = MediaCurrentTime;
                if (mediaCurrentTime < mediaCurrentTimeMax)
                    MediaCurrentTime = TimeSpanExtensions.Min(mediaCurrentTime + elapsedTime, mediaCurrentTimeMax);
            }
            lastLoopTime = currentTime;
        }

        /// <summary>
        /// Should be called from working thread only (or add proper locks)
        /// </summary>
        protected override void SeekInternal()
        {
            if (audioSource.CanSeek)
            {
                audioSource.Seek(commandSeekTime, out var flushHardwareBuffers);

                if (flushHardwareBuffers)
                {
                    storageBuffer.CountDataBytes = 0;

                    //To set the begin flag to true
                    PrepareInternal();
                    MediaCurrentTime = mediaCurrentTimeMax = TimeSpan.Zero;

                    //Seek
                    AudioLayer.SourceFlushBuffers(soundInstance.Source);
                }
            }
        }

        protected override void ExtractAndFillData()
        {
            //Try to extract some new audio data
            if (audioSource.ComputeAudioData(storageBuffer, out var endOfStream))
            {
                var bufferType = AudioLayer.BufferType.None;

                if (beginningOfStream)
                {
                    bufferType = AudioLayer.BufferType.BeginOfStream;
                    beginningOfStream = false;
                }

                SendExtractedAudioDataToAudioBuffer(bufferType);
                storageBuffer.CountDataBytes = 0;
            }

            isEof = endOfStream;  //setting this bool to true will let the media scheduler know when the audio media is done
        }

        private unsafe void SendExtractedAudioDataToAudioBuffer(AudioLayer.BufferType bufferType)
        {
            //Update the average number of bytes per buffer
            sentBuffersCount++;
            accumulatedSentBytesCount += storageBuffer.CountDataBytes;
            var countAverageBytesPerBuffer = (accumulatedSentBytesCount / sentBuffersCount);
            if (sentBuffersCount >= 10000)
            {
                //To prevent overflow
                sentBuffersCount = 10;
                accumulatedSentBytesCount = countAverageBytesPerBuffer * sentBuffersCount;
            }

            //new buffer's estimated time
            var bufferDuration = TimeSpan.FromSeconds(storageBuffer.CountDataBytes / byteRatePerSecond);

            //compute an estimate of the time left before this new buffer can be played
            var playTimeLeft = TimeSpan.FromSeconds((numberOfBuffers - freeBuffers.Count) * countAverageBytesPerBuffer / byteRatePerSecond);

            //Normally: if the buffer are continuous: bufferStartingTimeUs should be very close from the previous MediaPresentationTimeUsMax value
            //This could help us debug in the case of the audio timeFrame is incorrect

            var currentTime = MediaCurrentTime;
            var mediaCurrentTimeMin = storageBuffer.PresentationTime - playTimeLeft;
            mediaCurrentTimeMax = storageBuffer.PresentationTime + bufferDuration;

            //A buffer was being played, so we expect the currentTime to be between [min, bufferStartingTime]
            if (currentTime > mediaCurrentTimeMin && mediaCurrentTimeMin < storageBuffer.PresentationTime)
                mediaCurrentTimeMin = MediaCurrentTime;

            MediaCurrentTime = mediaCurrentTimeMin;

            FillBuffer(storageBuffer.Data.ByteBuffer, storageBuffer.CountDataBytes, bufferType);
        }
    }
}
