// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;

namespace Stride.Core.Serialization.Contents;

/// <summary>
/// Describe a reference between an object and another.
/// </summary>
/// <remarks>This class is IEquatable, and equality is true if and only if Location and ObjType properties match</remarks>
[DataSerializer(typeof(Serializer))]
public readonly struct ChunkReference : IEquatable<ChunkReference>
{
    public readonly string Location;

    public readonly Type ObjectType;

    public const int NullIdentifier = -1;

    public ChunkReference(Type objectType, string location)
    {
        ObjectType = objectType;
        Location = location;
    }

    public readonly bool Equals(ChunkReference other)
    {
        return Location == other.Location && ObjectType == other.ObjectType;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is ChunkReference reference && Equals(reference);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Location, ObjectType);
    }

    public static bool operator ==(ChunkReference left, ChunkReference right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ChunkReference left, ChunkReference right)
    {
        return !left.Equals(right);
    }

    internal class Serializer : DataSerializer<ChunkReference>
    {
        public override Type SerializationType
        {
            get { return typeof(ChunkReference); }
        }

        public override void Serialize(ref ChunkReference chunkReference, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(chunkReference.ObjectType.AssemblyQualifiedName);
                stream.Write(chunkReference.Location);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                string typeName = stream.ReadString();
                chunkReference = new ChunkReference(AssemblyRegistry.GetType(typeName), stream.ReadString());
            }
        }
    }
}
