// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Stride.Core;
using Stride.Media;

namespace Stride.Audio
{
    public interface ICustomBufferAudioSource
    {
        bool ComputeAudioData(AudioDataBuffer storageBufferToFill, out bool endOfStream);

        int Blocks { get; }
        int BlockSize { get; }
        int NativeBlockSize { get; }
        int Channels { get; }
        int SampleRate { get; }
    }

    public class AudioDataBuffer
    {
        public readonly int MaxBufferSizeBytes;

        public byte[] Data;

        public int CountDataBytes = 0;
        public TimeSpan PresentationTime;

        public AudioDataBuffer(int maxBufferSizeBytes)
        {
            MaxBufferSizeBytes = maxBufferSizeBytes;
            Data = new byte[MaxBufferSizeBytes];
        }
    }

    //The audio buffer is created by a callback
    public class CustomBufferSoundSource : StreamedBufferSoundSourceBase
    {
        /// <summary>
        /// Specifies how much data we wait to have extracted before we send the storage buffer to the audio buffer
        /// </summary>
        private int minBufferSizeBytesBeforeFlushingStorageBuffer = 28000;
        private int numberOfBuffers = 1;

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
        private AudioDataBuffer storageBuffer;

        private bool beginningOfStream;

        private PlayRange playRange;
        private volatile bool looped;

        private DateTime lastLoopTime = DateTime.Now;

        public override int MaxNumberOfBuffers => audioSource.Blocks;

        public CustomBufferSoundSource(SoundInstanceStreamedBuffer instance, ICustomBufferAudioSource customBufferAudioSource)
            : base(instance, customBufferAudioSource.Blocks, customBufferAudioSource.NativeBlockSize)
        {
            audioSource = customBufferAudioSource;
            numberOfBuffers = audioSource.Blocks;
            minBufferSizeBytesBeforeFlushingStorageBuffer = audioSource.BlockSize;
            storageBuffer = new AudioDataBuffer(audioSource.BlockSize);
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
            storageBuffer.CountDataBytes = 0;

            //To set the begin flag to true
            PrepareInternal();
            MediaCurrentTime = mediaCurrentTimeMax = TimeSpan.Zero;

            //Seek
            AudioLayer.SourceFlushBuffers(soundInstance.Source);
        }

        protected override void ExtractAndFillData()
        {
            //Try to extract some new audio data
            if (audioSource.ComputeAudioData(storageBuffer, out var endOfStream))
            {
                //Can we flush the storage buffer?
                if (storageBuffer.CountDataBytes >= minBufferSizeBytesBeforeFlushingStorageBuffer)
                {
                    var bufferType = AudioLayer.BufferType.None;

                    if (beginningOfStream)
                    {
                        bufferType = AudioLayer.BufferType.BeginOfStream;
                        beginningOfStream = false;
                    }
                    //We don't use an enfOfLoop or endOfStream type: we don't know what the mediaScheduler will ask us to do when we arrive at the end

                    SendExtractedAudioDataToAudioBuffer(bufferType);
                    storageBuffer.CountDataBytes = 0;

                }
            }

            isEof = endOfStream;  //setting this bool to true will let the media scheduler know when the audio media is done
        }

        protected override void DisposeInternal()
        {
            base.DisposeInternal();

        }

        private unsafe void SendExtractedAudioDataToAudioBuffer(AudioLayer.BufferType bufferType)
        {
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
            }

            FillBuffer(storageBuffer.Data, storageBuffer.CountDataBytes, bufferType);
        }
    }
}
