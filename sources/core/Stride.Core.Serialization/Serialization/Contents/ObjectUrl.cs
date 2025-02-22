// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Serialization.Contents;

[DataContract, Serializable]
[DataSerializer(typeof(Serializer))]
public readonly struct ObjectUrl : IEquatable<ObjectUrl>
{
    public static readonly ObjectUrl Empty = new(UrlType.None, string.Empty);

    public readonly UrlType Type;
    public readonly string Path;

    public ObjectUrl(UrlType type, string path)
    {
        Type = type;
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public readonly bool Equals(ObjectUrl other)
    {
        return Type == other.Type && string.Equals(Path, other.Path);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is ObjectUrl url && Equals(url);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Type, Path);
    }

    public static bool operator ==(ObjectUrl left, ObjectUrl right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ObjectUrl left, ObjectUrl right)
    {
        return !left.Equals(right);
    }

    public override readonly string ToString()
    {
        return Path;
    }

    internal class Serializer : DataSerializer<ObjectUrl>
    {
        public override void Serialize(ref ObjectUrl obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Type);
                stream.Write(obj.Path);
            }
            else
            {
                var type = stream.Read<UrlType>();
                var path = stream.ReadString();
                obj = new ObjectUrl(type, path);
            }
        }
    }
}
