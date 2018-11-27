// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Audio
{
    /// <summary>
    /// Used internally to serialize Sound
    /// </summary>
    internal sealed class SoundSerializer : DataSerializer<Sound>
    {
        public override void Serialize(ref Sound obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var audioEngine = services.GetService<IAudioEngineProvider>()?.AudioEngine;

                obj.FileProvider = services.GetService<IDatabaseFileProviderService>()?.FileProvider;
                obj.CompressedDataUrl = stream.ReadString();
                obj.SampleRate = stream.ReadInt32();
                obj.Channels = stream.ReadByte();
                obj.StreamFromDisk = stream.ReadBoolean();
                obj.Spatialized = stream.ReadBoolean();
                obj.NumberOfPackets = stream.ReadInt32();
                obj.MaxPacketLength = stream.ReadInt16();
                obj.Samples = stream.ReadInt32();

                if (!obj.StreamFromDisk && audioEngine != null && audioEngine.State != AudioEngineState.Invalidated && audioEngine.State != AudioEngineState.Disposed) //immediatelly preload all the data and decode
                {
                    obj.LoadSoundInMemory();
                }

                if (audioEngine != null)
                {
                    obj.Attach(audioEngine);
                }
            }
            else
            {
                stream.Write(obj.CompressedDataUrl);
                stream.Write(obj.SampleRate);
                stream.Write((byte)obj.Channels);
                stream.Write(obj.StreamFromDisk);
                stream.Write(obj.Spatialized);
                stream.Write(obj.NumberOfPackets);
                stream.Write((short)obj.MaxPacketLength);
                stream.Write(obj.Samples);
            }
        }
    }
}
