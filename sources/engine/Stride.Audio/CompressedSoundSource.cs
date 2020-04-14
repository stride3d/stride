// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Media;

namespace Stride.Audio
{
    internal sealed class CompressedSoundSource : DynamicSoundSource
    {
        private const int SamplesPerBuffer = 32768;
        private const int MaxChannels = 2;
        internal const int NumberOfBuffers = 4;
        internal const int SamplesPerFrame = 512;

        private static UnmanagedArray<short> utilityBuffer = new UnmanagedArray<short>(SamplesPerBuffer * MaxChannels);

        private Stream compressedSoundStream;
        private BinarySerializationReader reader;
        private volatile bool looped;
        private readonly int numberOfPackets;
        private int currentPacketIndex;
        private int startingPacketIndex;
        private int endPacketIndex;
        private PlayRange playRange;
        private int startPktSampleIndex;
        private int endPktSampleIndex;
        private bool begin;
        private readonly object rangeLock = new object();

        private Celt decoder;

        private readonly IVirtualFileProvider fileProvider;
        private readonly string soundStreamUrl;

        private readonly int channels;
        private readonly int sampleRate;
        private readonly int samples;

        private readonly int maxCompressedSize;
        private byte[] compressedBuffer;

        //==========================================================================================
        //==========================================================================================
        //PROTOTYPE

        private byte[] byteBuffer = null;
        private int byteBufferCurrentPosition = 0;

        public CompressedSoundSource(SoundInstance instance, byte[] byteBuffer, int numberOfPackets, int numberOfSamples, int sampleRate, int channels, int maxCompressedSize) 
            : base(instance, NumberOfBuffers, SamplesPerBuffer * MaxChannels * sizeof(short))
        {
            looped = instance.IsLooping;
            this.channels = channels;
            this.maxCompressedSize = maxCompressedSize;
            this.soundStreamUrl = null;
            this.byteBuffer = byteBuffer;
            this.sampleRate = sampleRate;
            this.numberOfPackets = numberOfPackets;
            this.samples = numberOfSamples;
            playRange = new PlayRange(TimeSpan.Zero, TimeSpan.Zero);

            NewSources.Add(this);
        }

        //==========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedSoundSource"/> class.
        /// This type of DynamicSoundSource is streamed from Disk and reads compressed Celt encoded data, used internally.
        /// </summary>
        /// <param name="instance">The associated SoundInstance</param>
        /// <param name="fileProvider">The file provider to read the stream from</param>
        /// <param name="soundStreamUrl">The compressed stream internal URL</param>
        /// <param name="numberOfPackets">The number of packets</param>
        /// <param name="sampleRate">The sample rate of the compressed data</param>
        /// <param name="channels">The number of channels of the compressed data</param>
        /// <param name="maxCompressedSize">The maximum size of a compressed packet</param>
        public CompressedSoundSource(SoundInstance instance, IVirtualFileProvider fileProvider, string soundStreamUrl, int numberOfPackets, int numberOfSamples, int sampleRate, int channels, int maxCompressedSize) 
            : base(instance, NumberOfBuffers, SamplesPerBuffer * MaxChannels * sizeof(short))
        {
            looped = instance.IsLooping;
            this.channels = channels;
            this.maxCompressedSize = maxCompressedSize;
            this.fileProvider = fileProvider;
            this.soundStreamUrl = soundStreamUrl;
            this.sampleRate = sampleRate;
            this.numberOfPackets = numberOfPackets;
            this.samples = numberOfSamples;
            playRange = new PlayRange(TimeSpan.Zero, TimeSpan.Zero);

            NewSources.Add(this);
        }

        /// <summary>
        /// Gets the max numbers of buffered buffers
        /// </summary>
        public override int MaxNumberOfBuffers => NumberOfBuffers;

        /// <summary>
        /// Sets if the stream should be played in loop
        /// </summary>
        /// <param name="loop">if looped or not</param>
        public override void SetLooped(bool loop)
        {
            looped = loop;
        }

        /// <inheritdoc/>
        public override PlayRange PlayRange
        {
            get
            {
                lock (rangeLock)
                {
                    return playRange;
                }
            }
            set
            {
                lock (rangeLock)
                {
                    playRange = value;
                }

                base.PlayRange = value;
            }
        }

        protected override void InitializeInternal()
        {
            if (soundStreamUrl != null)
            {
                compressedSoundStream = fileProvider.OpenStream(soundStreamUrl, VirtualFileMode.Open, VirtualFileAccess.Read, VirtualFileShare.Read, StreamFlags.Seekable);
                decoder = new Celt(sampleRate, SamplesPerFrame, channels, true);
                compressedBuffer = new byte[maxCompressedSize];
                reader = new BinarySerializationReader(compressedSoundStream);

                base.InitializeInternal();
            }
        }

        protected override void PrepareInternal()
        {
            base.PrepareInternal();

            begin = true;
            if (byteBuffer != null) return;

            compressedSoundStream.Position = 0;
            currentPacketIndex = 0;
            startPktSampleIndex = 0;
            endPktSampleIndex = 0;
            endPacketIndex = numberOfPackets;

            PlayRange range;
            lock (rangeLock)
            {
                range = playRange;
            }

            // Reset decoder state
            decoder.ResetDecoder();

            // Ignore invalid data at beginning (due to encoder delay) & end of stream (due to packet size)
            var samplesToSkip = decoder.GetDecoderSampleDelay();

            // Compute boundaries
            var sampleBegin = (channels * samplesToSkip);
            var sampleEnd = sampleBegin + (channels * samples);

            var frameSize = SamplesPerFrame * channels;
            //ok we need to handle this case properly, this means that the user wants to use a different then full audio stream range...
            var sampleStart = sampleBegin + (int)Math.Floor(sampleRate * (double)channels * range.Start.TotalSeconds);
            // Make sure start is at least one sample before the end to avoid edge cases where startingPacketIndex == numberOfPackets
            sampleStart = Math.Min(Math.Max(sampleStart, sampleBegin), sampleEnd - 1 * channels);

            var sampleStop = sampleBegin
                + (range.Length != TimeSpan.Zero
                    ? (int)Math.Floor(sampleRate * (double)channels * range.End.TotalSeconds)
                    : (channels * samples));
            // Make sure stop is at least one sample after start
            sampleStop = Math.Min(Math.Max(sampleStop, sampleStart + 1 * channels), sampleEnd);

            // Compute start/end packet
            startingPacketIndex = sampleStart / frameSize;
            endPacketIndex = (sampleStop - 1) / frameSize; // -1 to make sure we stay in last packet if using all of it

            // How much data to use in start/end packet
            startPktSampleIndex = sampleStart % (frameSize);
            endPktSampleIndex = frameSize - sampleStop % frameSize;

            // skip to the starting packet
            if (startingPacketIndex < numberOfPackets && endPacketIndex < numberOfPackets && startingPacketIndex <= endPacketIndex) // this shouldn't happen anymore with the min/max clamps
            {
                //valid offsets.. process it
                var skipCounter = startingPacketIndex;
                while (skipCounter-- > 0)
                {
                    //skip data to reach starting packet
                    var len = reader.ReadInt16();
                    compressedSoundStream.Position = compressedSoundStream.Position + len;
                    currentPacketIndex++;
                }
            }
        }

        /// <summary>
        /// Destroys the instance.
        /// </summary>
        protected override void DisposeInternal()
        {
            base.DisposeInternal();
            compressedSoundStream.Dispose();
            decoder.Dispose();
        }

        protected override unsafe void ExtractAndFillData()
        {
            if (byteBuffer != null)
            {
                int maxSize = 100 * 1000;
                int bufferLen = byteBuffer.Length;
                int remainingLen = bufferLen - byteBufferCurrentPosition;

                int countByteTransfered = remainingLen > maxSize ? maxSize : remainingLen;
                short[] sdata = new short[(int)Math.Ceiling((decimal)(countByteTransfered / 2))];
                Buffer.BlockCopy(byteBuffer, byteBufferCurrentPosition, sdata, 0, countByteTransfered);
                byteBufferCurrentPosition += countByteTransfered;

                bool endingPacket = byteBufferCurrentPosition == bufferLen;

                var bufferType = AudioLayer.BufferType.None;
                if (endingPacket)
                {
                    bufferType = looped ? AudioLayer.BufferType.EndOfLoop : AudioLayer.BufferType.EndOfStream;
                }
                else if (begin)
                {
                    bufferType = AudioLayer.BufferType.BeginOfStream;
                    begin = false;
                }

                FillBuffer(sdata, countByteTransfered, bufferType);
            }
            else
            {
                const int passes = SamplesPerBuffer / SamplesPerFrame;
                var offset = 0;
                var bufferPtr = (short*)utilityBuffer.Pointer;
                var startingPacket = startingPacketIndex == currentPacketIndex;
                var endingPacket = false;
                for (var i = 0; i < passes; i++)
                {
                    endingPacket = endPacketIndex == currentPacketIndex;

                    //read one packet, size first, then data
                    var len = reader.ReadInt16();
                    compressedSoundStream.Read(compressedBuffer, 0, len);
                    currentPacketIndex++;

                    var writePtr = bufferPtr + offset;
                    if (decoder.Decode(compressedBuffer, len, writePtr) != SamplesPerFrame)
                    {
                        throw new Exception("Celt decoder returned a wrong decoding buffer size.");
                    }

                    offset += SamplesPerFrame * channels;

                    if (endingPacket || compressedSoundStream.Position == compressedSoundStream.Length)
                    {
                        break;
                    }
                }

                // Send buffer to hardware
                var finalPtr = new IntPtr(bufferPtr + (startingPacket ? startPktSampleIndex : 0));
                var finalSize = (offset - (startingPacket ? startPktSampleIndex : 0) - (endingPacket ? endPktSampleIndex : 0)) * sizeof(short);

                var bufferType = AudioLayer.BufferType.None;
                if (endingPacket)
                {
                    bufferType = looped ? AudioLayer.BufferType.EndOfLoop : AudioLayer.BufferType.EndOfStream;
                }
                else if (begin)
                {
                    bufferType = AudioLayer.BufferType.BeginOfStream;
                    begin = false;
                }
                FillBuffer(finalPtr, finalSize, bufferType);

                // Go back to beginning if necessary
                if (endingPacket || compressedSoundStream.Position == compressedSoundStream.Length)
                {
                    if (looped) //prepare again to play from begin
                    {
                        PrepareInternal();
                    }
                    else // stops the sound
                    {
                        StopInternal(false);
                    }
                }
            }
        }
    }
}
