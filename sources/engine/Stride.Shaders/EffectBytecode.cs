// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Shaders;

[DataContract]
[ContentSerializer(typeof(DataContentSerializer<EffectBytecode>))]
public sealed class EffectBytecode
{
    /// <summary>
    /// Contains a compiled shader with bytecode for each stage.
    /// </summary>
        /// <summary>
        /// Magic header stored in front of an effect bytecode to avoid reading old versions.
        /// </summary>
        /// <remarks>
        /// If EffectBytecode is changed, this number must be changed manually.
        /// </remarks>
        /// <summary>
        /// The reflection from the bytecode.
        /// </summary>
        /// <summary>
        /// The used sources
        /// </summary>
        /// <summary>
        /// The bytecode for each stage.
        /// </summary>
        /// <summary>
        /// Computes a unique identifier for this bytecode instance.
        /// </summary>
        /// <returns>ObjectId.</returns>
    public const uint MagicHeader = 0xEFFEC007;  // NOTE: If EffectBytecode is changed, this number must be changed manually


    public EffectReflection Reflection;

    public HashSourceCollection HashSources;

    public ShaderBytecode[] Stages;

        /// <summary>
        /// Loads an <see cref="EffectBytecode" /> from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>EffectBytecode.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>

        /// <summary>
        /// Loads an <see cref="EffectBytecode" /> from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>EffectBytecode.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>

        /// <summary>
        /// Loads an <see cref="EffectBytecode" /> from a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>EffectBytecode or null if the magic header is not matching</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>

    public ObjectId ComputeId()
    {
        var effectBytecode = this;

        // We should most of the time have stages, unless someone is calling this method on a new EffectBytecode
        if (effectBytecode.Stages is not null)
        {
            effectBytecode = (EffectBytecode) MemberwiseClone();

            effectBytecode.Stages = (ShaderBytecode[]) effectBytecode.Stages.Clone();

            // Because ShaderBytecode.Data can vary, we are calculating the bytecodeId only with the ShaderBytecode.Id
            for (int i = 0; i < effectBytecode.Stages.Length; i++)
            {
                var newStage = effectBytecode.Stages[i].Clone();
                effectBytecode.Stages[i] = newStage;
                newStage.Data = null;
            }
        }

        /// <summary>
        /// Writes this <see cref="EffectBytecode" /> to a stream with its magic number.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        // TODO: Optimize: Pre-calculate bytecodeId in order to avoid writing to same storage
        ObjectId newBytecodeId;
        var memStream = new MemoryStream();
        using (var stream = new DigestStream(memStream))
        {
            effectBytecode.WriteTo(stream);
            newBytecodeId = stream.CurrentHash;
        }
        return newBytecodeId;
    }

    public static EffectBytecode FromBytes(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        return FromStream(new MemoryStream(buffer));
    }

    public static EffectBytecode FromBytesSafe(byte[] buffer)
    {
        var result = FromBytes(buffer);
        return result ?? throw new ArgumentException($"{nameof(buffer)} contains invalid Effect bytecode. Could not find magic header 0x{MagicHeader:X}.");
    }

    public static EffectBytecode FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var reader = new BinarySerializationReader(stream);
        var version = reader.Read<uint>();

        // Version is not matching, return null
        if (version != MagicHeader)
            return null;

        return reader.Read<EffectBytecode>();
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var writer = new BinarySerializationWriter(stream);
        writer.Write(MagicHeader);
        writer.Write(this);
    }
}
