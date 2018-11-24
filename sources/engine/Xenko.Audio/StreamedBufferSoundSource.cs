// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Xenko.Core;
using Xenko.Media;

namespace Xenko.Audio
{
    //The audio buffer is extracted by an external, unknown API (MediaCodec, FFMPEG, ...), and then feed into this class
    public partial class StreamedBufferSoundSource : DynamicSoundSource, IMediaExtractor
    {
        /// <summary>
        /// Specifies how much data we wait to have extracted before we send the storage buffer to the audio buffer
        /// </summary>
        private const int MinBufferSizeBytesBeforeFlushingStorageBuffer = 28000;
        private const int MaxBufferSizeBytes = 64000;
        private const int NumberOfBuffers = 4;

        private readonly object objLock = new object();

        private readonly MediaSynchronizer mediaSynchronizer;
        private readonly string mediaDataUrl;
        private readonly long startPosition;
        private readonly long length;
        private TimeSpan mediaCurrentTimeMax;
        private TimeSpan mediaCurrentTime;
        private TimeSpan commandSeekTime;

        private int sentBuffersCount = 0;
        private int accumulatedSentBytesCount = 0;

        private float byteRatePerSecond; //bytes per second

        /// <summary>
        /// Temporary buffers for accumulating the data we're extracting before sending them to the AudioLayer
        /// </summary>
        private AudioDataStorageBuffer storageBuffer = new AudioDataStorageBuffer();

        private volatile bool isEof;
        private bool beginningOfStream;

        private PlayRange playRange;
        private volatile bool looped;

        private DateTime lastLoopTime = DateTime.Now;

        /// <summary>
        /// The media scheduler will check this field to determine whether he can stop waiting for the extractors getting ready
        /// </summary>
        private volatile bool seekRequestCompleted = true;

        private class AudioDataStorageBuffer
        {
            public byte[] Data = new byte[MaxBufferSizeBytes];

            public int CountDataBytes = 0;
            public TimeSpan PresentationTime;
        }

        public int Channels { get; private set; }

        public int SampleRate { get; private set; }

        public MediaType MediaType => MediaType.Audio;

        public TimeSpan MediaDuration { get; private set; }

        public override int MaxNumberOfBuffers => NumberOfBuffers;

        public bool IsDisposed => isDisposed;

        public float SpeedFactor { get; set; } = 1f;

        public StreamedBufferSoundSource(SoundInstanceStreamedBuffer instance, MediaSynchronizer synchronizer, string mediaDataUrl, long startPosition, long length)
            : base(instance, NumberOfBuffers, MaxBufferSizeBytes)
        {
            mediaSynchronizer = synchronizer;
            this.mediaDataUrl = mediaDataUrl;
            this.startPosition = startPosition;
            this.length = length;

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

        /// <inheritdoc/>
        public override PlayRange PlayRange
        {
            get
            {
                lock (objLock)
                {
                    return playRange;
                }
            }
            set
            {
                lock (objLock)
                {
                    playRange = value;
                }

                base.PlayRange = value;
            }
        }

        public bool SeekRequestCompleted()
        {
            return seekRequestCompleted;
        }
        public bool ReachedEndOfMedia()
        {
            return isEof;
        }

        public void Seek(TimeSpan mediaTime)
        {
            seekRequestCompleted = false;
            commandSeekTime = mediaTime;
            Commands.Enqueue(AsyncCommand.Seek);
        }

        protected override void InitializeInternal()
        {
            InitializeMediaExtractor(mediaDataUrl, startPosition, length);

            byteRatePerSecond = SampleRate * (Channels * 2); //2 = bit depth (Pcm16bit)

            base.InitializeInternal();
        }

        partial void InitializeMediaExtractor(string mediaDataUrl, long startPosition, long length);

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
            SeekInternalImpl(commandSeekTime);
        }

        partial void SeekInternalImpl(TimeSpan seekTimeUs);

        protected override void ExtractAndFillData()
        {
            //Try to extract some new audio data
            if (ExtractSomeAudioData(out var endOfFile))
            {
                //Can we flush the storage buffer?
                if (storageBuffer.CountDataBytes >= MinBufferSizeBytesBeforeFlushingStorageBuffer)
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

                    if (mediaSynchronizer.IsWaitingForSynchronization())
                        seekRequestCompleted = true;
                }
            }

            isEof = endOfFile;  //setting this bool to true will let the media scheduler know when the audio media is done
        }

        protected override void DisposeInternal()
        {
            base.DisposeInternal();

            ReleaseMediaInternal();
        }

        partial void ReleaseMediaInternal();

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
                var playTimeLeft = TimeSpan.FromSeconds((NumberOfBuffers - freeBuffers.Count) * countAverageBytesPerBuffer / byteRatePerSecond);

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
