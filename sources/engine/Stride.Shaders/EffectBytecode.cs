// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Shaders;

/// <summary>
///   Represents a compiled Effect with bytecode for each Shader stage (vertex, pixel, geometry, etc.)
/// </summary>
[DataContract]
[ContentSerializer(typeof(DataContentSerializer<EffectBytecode>))]
public sealed class EffectBytecode
{
    /// <summary>
    ///   A constant value representing the <em>magic header</em> stored in front of an Effect bytecode
    ///   to avoid reading old versions.
    /// </summary>
    public const uint MagicHeader = 0xEFFEC007;  // NOTE: If EffectBytecode is changed, this number must be changed manually


    /// <summary>
    ///   The reflection data extracted from the Effect bytecode.
    /// </summary>
    public EffectReflection Reflection;

    /// <summary>
    ///   A collection of each of the Effect Shader source URIs and their associated <see cref="ObjectId"/>s.
    /// </summary>
    public HashSourceCollection HashSources;

    /// <summary>
    ///   The Effect bytecode for each of the Shader stages.
    /// </summary>
    public ShaderBytecode[] Stages;


    /// <summary>
    ///   Computes a unique identifier for the Effect bytecode.
    /// </summary>
    /// <returns>An unique <see cref="ObjectId"/> for the Effect bytecode.</returns>
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

    /// <summary>
    ///   Loads an <see cref="EffectBytecode"/> from a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    /// <returns>The loaded Effect bytecode.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
    public static EffectBytecode FromBytes(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        return FromStream(new MemoryStream(buffer));
    }

    /// <summary>
    ///   Loads an <see cref="EffectBytecode"/> from a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    /// <returns>The loaded Effect bytecode.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="buffer"/> contains invalid Effect bytecode.</exception>
    public static EffectBytecode FromBytesSafe(byte[] buffer)
    {
        var result = FromBytes(buffer);
        return result ?? throw new ArgumentException($"{nameof(buffer)} contains invalid Effect bytecode. Could not find magic header 0x{MagicHeader:X}.");
    }

    /// <summary>
    ///   Loads an <see cref="EffectBytecode"/> from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The loaded Effect bytecode, or <see langword="null"/> if the magic header is not matching.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    ///   Writes the <see cref="EffectBytecode"/> to a stream with its magic number.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var writer = new BinarySerializationWriter(stream);
        writer.Write(MagicHeader);
        writer.Write(this);
    }
}
