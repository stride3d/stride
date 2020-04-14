// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Audio
{
    /// <summary>
    /// Sound content.
    /// </summary>
    /// <remarks>
    /// Sound is played with a <see cref="SoundInstance"/>.
    /// </remarks>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    [ContentSerializer(typeof(DataContentSerializer<Sound>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Sound>), Profile = "Content")]
    [DataSerializer(typeof(SoundSerializer))]
    public sealed class Sound : SoundBase
    {
        internal bool StreamFromDisk { get; set; }

        internal string CompressedDataUrl { get; set; }

        [DataMemberIgnore]
        internal AudioLayer.Buffer PreloadedBuffer;

        internal IVirtualFileProvider FileProvider;

        internal int Samples { get; set; }

        /// <summary>
        /// Create a new sound effect instance of the sound effect. 
        /// The audio data are shared between the instances so that useless memory copies is avoided. 
        /// Each instance that can be played and localized independently from others.
        /// </summary>
        /// <returns>A new sound instance</returns>
        /// <exception cref="ObjectDisposedException">The sound has already been disposed</exception>
        public SoundInstance CreateInstance(AudioListener listener = null, bool forceLoadInMemory = false, bool useHrtf = false, float directionalFactor = 0.0f, HrtfEnvironment environment = HrtfEnvironment.Small)
        {
            if (listener == null)
            {
                listener = AudioEngine.DefaultListener;
            }

            CheckNotDisposed();

            var newInstance = new SoundInstance(this, listener, forceLoadInMemory, useHrtf, directionalFactor, environment) { Name = Name + " - Instance " + intancesCreationCount };
            RegisterInstance(newInstance);

            return newInstance;
        }

        public override SoundInstance CreateInstance(AudioListener listener = null, bool useHrtf = false, float directionalFactor = 0.0f, HrtfEnvironment environment = HrtfEnvironment.Small)
        {
            return CreateInstance(listener, false, useHrtf, directionalFactor, environment);
        }

        protected override void Destroy()
        {
            base.Destroy();

            if (AudioEngine == null || AudioEngine.State == AudioEngineState.Invalidated)
                return;

            if (!StreamFromDisk)
            {
                AudioLayer.BufferDestroy(PreloadedBuffer);
            }
        }

        internal void LoadSoundInMemory()
        {
            if (PreloadedBuffer.Ptr != IntPtr.Zero) return;

            using (var soundStream = FileProvider.OpenStream(CompressedDataUrl, VirtualFileMode.Open, VirtualFileAccess.Read, VirtualFileShare.Read, StreamFlags.Seekable))
            using (var decoder = new Celt(SampleRate, CompressedSoundSource.SamplesPerFrame, Channels, true))
            {
                var reader = new BinarySerializationReader(soundStream);
                var samplesPerPacket = CompressedSoundSource.SamplesPerFrame * Channels;

                PreloadedBuffer = AudioLayer.BufferCreate(samplesPerPacket * NumberOfPackets * sizeof(short));

                var memory = new UnmanagedArray<short>(samplesPerPacket * NumberOfPackets);

                var offset = 0;
                var outputBuffer = new short[samplesPerPacket];
                for (var i = 0; i < NumberOfPackets; i++)
                {
                    var len = reader.ReadInt16();
                    var compressedBuffer = reader.ReadBytes(len);
                    var samplesDecoded = decoder.Decode(compressedBuffer, len, outputBuffer);
                    memory.Write(outputBuffer, offset, 0, samplesDecoded * Channels);
                    offset += samplesDecoded * Channels * sizeof(short);
                }

                // Ignore invalid data at beginning (due to encoder delay) & end of stream (due to packet size)
                var samplesToSkip = decoder.GetDecoderSampleDelay();
                AudioLayer.BufferFill(PreloadedBuffer, memory.Pointer + samplesToSkip * Channels * sizeof(short), Samples * Channels * sizeof(short), SampleRate, Channels == 1);
                memory.Dispose();
            }
        }
    }
}
