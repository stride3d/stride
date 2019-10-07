// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;

namespace Xenko.Shaders
{
    /// <summary>
    /// Contains a compiled shader with bytecode for each stage.
    /// </summary>
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<EffectBytecode>))]
    public sealed class EffectBytecode
    {
        /// <summary>
        /// Magic header stored in front of an effect bytecode to avoid reading old versions.
        /// </summary>
        /// <remarks>
        /// If EffectBytecode is changed, this number must be changed manually.
        /// </remarks>
        public const uint MagicHeader = 0xEFFEC007;

        /// <summary>
        /// The reflection from the bytecode.
        /// </summary>
        public EffectReflection Reflection;

        /// <summary>
        /// The used sources
        /// </summary>
        public HashSourceCollection HashSources;

        /// <summary>
        /// The bytecode for each stage.
        /// </summary>
        public ShaderBytecode[] Stages;

        /// <summary>
        /// Computes a unique identifier for this bytecode instance.
        /// </summary>
        /// <returns>ObjectId.</returns>
        public ObjectId ComputeId()
        {
            var effectBytecode = this;

            // We should most of the time have stages, unless someone is calling this method on a new EffectBytecode
            if (effectBytecode.Stages != null)
            {
                effectBytecode = (EffectBytecode)MemberwiseClone();

                effectBytecode.Stages = (ShaderBytecode[])effectBytecode.Stages.Clone();

                // Because ShaderBytecode.Data can vary, we are calculating the bytecodeId only with the ShaderBytecode.Id.
                for (int i = 0; i < effectBytecode.Stages.Length; i++)
                {
                    var newStage = effectBytecode.Stages[i].Clone();
                    effectBytecode.Stages[i] = newStage;
                    newStage.Data = null;
                }
            }

            // Not optimized: Pre-calculate bytecodeId in order to avoid writing to same storage
            ObjectId newBytecodeId;
            var memStream = new MemoryStream();
            using (var stream = new DigestStream(memStream))
            {
                effectBytecode.WriteTo(stream);
                newBytecodeId = stream.CurrentHash;
            }
            return newBytecodeId;
        }

        /// <summary>
        /// Loads an <see cref="EffectBytecode" /> from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>EffectBytecode.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        public static EffectBytecode FromBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            return FromStream(new MemoryStream(buffer));
        }

        /// <summary>
        /// Loads an <see cref="EffectBytecode" /> from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>EffectBytecode.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        public static EffectBytecode FromBytesSafe(byte[] buffer)
        {
            var result = FromBytes(buffer);
            if (result == null)
            {
                throw new ArgumentException("Invalid effect buffer bytecode. Magic number is not matching.");
            }
            return result;
        }

        /// <summary>
        /// Loads an <see cref="EffectBytecode" /> from a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>EffectBytecode or null if the magic header is not matching</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static EffectBytecode FromStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            var reader = new BinarySerializationReader(stream);
            var version = reader.Read<uint>();
            // Version is not matching, return null
            if (version != MagicHeader)
            {
                return null;
            }
            return reader.Read<EffectBytecode>();
        }

        /// <summary>
        /// Writes this <see cref="EffectBytecode" /> to a stream with its magic number.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void WriteTo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            var writer = new BinarySerializationWriter(stream);
            writer.Write(MagicHeader);
            writer.Write(this);
        }
    }
}
