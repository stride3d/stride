// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Media;

namespace Xenko.Audio
{
    internal sealed class CompressedSoundSource : DynamicSoundSource
    {
        private const int SamplesPerBuffer = 32768;
        private const int MaxChannels = 2;
        internal const int NumberOfBuffers = 4;
        internal const int SamplesPerFrame = 512;

        private static UnmanagedArray<short> UtilityBuffer = new UnmanagedArray<short>(SamplesPerBuffer * MaxChannels);

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

        private readonly string soundStreamUrl;

        private readonly int channels;
        private readonly int sampleRate;

        private readonly int maxCompressedSize;
        private byte[] compressedBuffer;

        //==========================================================================================
        //==========================================================================================
        //PROTOTYPE

        private byte[] ByteBuffer = null;
        private int ByteBufferCurrentPosition = 0;

        public CompressedSoundSource(SoundInstance instance, byte[] byteBuffer, int numberOfPackets, int sampleRate, int channels, int maxCompressedSize) : 
            base(instance, NumberOfBuffers, SamplesPerBuffer * MaxChannels * sizeof(short))
        {
            looped = instance.IsLooping;
            this.channels = channels;
            this.maxCompressedSize = maxCompressedSize;
            this.soundStreamUrl = null;
            this.ByteBuffer = byteBuffer;
            this.sampleRate = sampleRate;
            this.numberOfPackets = numberOfPackets;
            playRange = new PlayRange(TimeSpan.Zero, TimeSpan.Zero);


            NewSources.Add(this);
        }

        //==========================================================================================

        /// <summary>
        /// This type of DynamicSoundSource is streamed from Disk and reads compressed Celt encoded data, used internally.
        /// </summary>
        /// <param name="instance">The associated SoundInstance</param>
        /// <param name="soundStreamUrl">The compressed stream internal URL</param>
        /// <param name="numberOfPackets"></param>
        /// <param name="sampleRate">The sample rate of the compressed data</param>
        /// <param name="channels">The number of channels of the compressed data</param>
        /// <param name="maxCompressedSize">The maximum size of a compressed packet</param>
        public CompressedSoundSource(SoundInstance instance, string soundStreamUrl, int numberOfPackets, int sampleRate, int channels, int maxCompressedSize) : 
            base(instance, NumberOfBuffers, SamplesPerBuffer * MaxChannels * sizeof(short))
        {
            looped = instance.IsLooping;
            this.channels = channels;
            this.maxCompressedSize = maxCompressedSize;
            this.soundStreamUrl = soundStreamUrl;
            this.sampleRate = sampleRate;
            this.numberOfPackets = numberOfPackets;
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

        /// <summary>
        /// Sets the range of the sound to play.
        /// </summary>
        /// <param name="range">a PlayRange structure that describes the starting offset and ending point of the sound to play in seconds.</param>
        public override void SetPlayRange(PlayRange range)
        {
            lock (rangeLock)
            {
                playRange = range;
            }

            base.SetPlayRange(range);
        }

        protected override void InitializeInternal()
        {
            if (soundStreamUrl != null)
            {
                compressedSoundStream = ContentManager.FileProvider.OpenStream(soundStreamUrl, VirtualFileMode.Open, VirtualFileAccess.Read, VirtualFileShare.Read, StreamFlags.Seekable);
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
            if (ByteBuffer != null) return;

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

            if (range.Start != TimeSpan.Zero || range.Length != TimeSpan.Zero)
            {
                var frameSize = SamplesPerFrame * channels;
                //ok we need to handle this case properly, this means that the user wants to use a different then full audio stream range...
                var sampleStart = sampleRate * (double)channels * range.Start.TotalSeconds;
                startPktSampleIndex = (int)Math.Floor(sampleStart) % (frameSize);

                var sampleStop = sampleRate * (double)channels * range.End.TotalSeconds;
                endPktSampleIndex = frameSize - (int)Math.Floor(sampleStart) % frameSize;

                var skipCounter = startingPacketIndex = (int)Math.Floor(sampleStart / frameSize);
                endPacketIndex = (int)Math.Floor(sampleStop / frameSize);

                // skip to the starting packet
                if (startingPacketIndex < numberOfPackets && endPacketIndex < numberOfPackets && startingPacketIndex < endPacketIndex)
                {
                    //valid offsets.. process it
                    while (skipCounter-- > 0)
                    {
                        //skip data to reach starting packet
                        var len = reader.ReadInt16();
                        compressedSoundStream.Position = compressedSoundStream.Position + len;
                        currentPacketIndex++;
                    }
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
            if (ByteBuffer != null)
            {
                int maxSize = 100*1000;
                int bufferLen = ByteBuffer.Length;
                int remainingLen = bufferLen - ByteBufferCurrentPosition;

                int countByteTransfered = remainingLen > maxSize ? maxSize : remainingLen;
                short[] sdata = new short[(int)Math.Ceiling((decimal)(countByteTransfered / 2))];
                Buffer.BlockCopy(ByteBuffer, ByteBufferCurrentPosition, sdata, 0, countByteTransfered);
                ByteBufferCurrentPosition += countByteTransfered;

                bool endingPacket = ByteBufferCurrentPosition == bufferLen;

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
                var bufferPtr = (short*)UtilityBuffer.Pointer;
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
                        if (looped) //prepare again to play from begin
                        {
                            PrepareInternal();
                        }
                        else // stops the sound
                        {
                            StopInternal();
                        }

                        break;
                    }
                }

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
            }
        }
    }
}
