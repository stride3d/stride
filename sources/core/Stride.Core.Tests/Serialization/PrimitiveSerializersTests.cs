// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;
using System.IO;

namespace Stride.Core.Tests.Serialization;

public class PrimitiveSerializersTests
{
    [Fact]
    public void DateTimeSerializer_SerializesAndDeserializes()
    {
        var serializer = new DateTimeSerializer();
        var original = new DateTime(2025, 12, 6, 10, 30, 45);

        var (deserialized, _) = SerializeDeserialize(serializer, original);

        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void DateTimeSerializer_HandlesMinValue()
    {
        var serializer = new DateTimeSerializer();
        var original = DateTime.MinValue;

        var (deserialized, _) = SerializeDeserialize(serializer, original);

        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void DateTimeSerializer_HandlesMaxValue()
    {
        var serializer = new DateTimeSerializer();
        var original = DateTime.MaxValue;

        var (deserialized, _) = SerializeDeserialize(serializer, original);

        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void TimeSpanSerializer_SerializesAndDeserializes()
    {
        var serializer = new TimeSpanSerializer();
        var original = TimeSpan.FromHours(2.5);

        var (deserialized, _) = SerializeDeserialize(serializer, original);

        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void TimeSpanSerializer_HandlesZero()
    {
        var serializer = new TimeSpanSerializer();
        var original = TimeSpan.Zero;

        var (deserialized, _) = SerializeDeserialize(serializer, original);

        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void TimeSpanSerializer_HandlesNegativeValues()
    {
        var serializer = new TimeSpanSerializer();
        var original = TimeSpan.FromMinutes(-30);

        var (deserialized, _) = SerializeDeserialize(serializer, original);

        Assert.Equal(original, deserialized);
    }

    private static (T, byte[]) SerializeDeserialize<T>(DataSerializer<T> serializer, T value)
    {
        using var memoryStream = new MemoryStream();
        var writer = new BinarySerializationWriter(memoryStream);

        // Serialize
        serializer.Serialize(ref value, ArchiveMode.Serialize, writer);

        // Get bytes
        var bytes = memoryStream.ToArray();

        // Deserialize
        memoryStream.Position = 0;
        var reader = new BinarySerializationReader(memoryStream);
        var result = default(T)!;
        serializer.Serialize(ref result, ArchiveMode.Deserialize, reader);

        return (result, bytes);
    }
}
