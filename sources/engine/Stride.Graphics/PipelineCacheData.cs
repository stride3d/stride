// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Text;
using Stride.Core.Storage;

namespace Stride.Graphics;

/// <summary>
///   The form of the data <see cref="GraphicsDevice.GetPipelineCacheData"/> returns and <see cref="GraphicsDevice.MergePipelineCache"/>
///   takes: the driver's blob in an envelope that tells a blob damaged on its way back (truncated, padded, overwritten)
///   from a whole one, and names the version of the driver that made it. A driver must not be given a damaged blob, and
///   the validation layers cannot check it (<c>VUID-VkPipelineCacheCreateInfo-initialDataSize-00768</c> and <c>-00769</c>
///   are unimplementable). A driver is to change its <c>pipelineCacheUUID</c> whenever it cannot read its earlier blobs,
///   and some do not, so the blob of another version of the driver is not given to it either.
/// </summary>
internal static class PipelineCacheData
{
    /// <summary>"SPLC", read as a little-endian integer.</summary>
    private const uint Magic = 0x434C5053;
    private const uint Version = 2;

    // The magic, the version, the blob's length, the blob's hash, then the hash of the driver's version
    private const int MagicOffset = 0;
    private const int VersionOffset = 4;
    private const int LengthOffset = 8;
    private const int HashOffset = 12;
    private const int DriverVersionOffset = HashOffset + ObjectId.HashSize;
    private const int HeaderSize = DriverVersionOffset + ObjectId.HashSize;

    /// <summary>Puts <paramref name="blob"/>, made by the driver of <paramref name="driverVersion"/>, in the envelope.</summary>
    public static byte[] Wrap(ReadOnlySpan<byte> blob, string driverVersion)
    {
        var data = new byte[HeaderSize + blob.Length];
        var header = data.AsSpan(0, HeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header[MagicOffset..], Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(header[VersionOffset..], Version);
        BinaryPrimitives.WriteInt32LittleEndian(header[LengthOffset..], blob.Length);
        WriteHash(ObjectId.FromBytes(blob), header[HashOffset..]);
        WriteHash(HashOf(driverVersion), header[DriverVersionOffset..]);
        blob.CopyTo(data.AsSpan(HeaderSize));
        return data;
    }

    /// <summary>
    ///   Finds the blob in the envelope, without copying it.
    /// </summary>
    /// <returns>
    ///   <see langword="false"/> if <paramref name="data"/> is not an envelope, its blob is not whole, or the blob was
    ///   made by another version of the driver than <paramref name="driverVersion"/>.
    /// </returns>
    public static bool TryUnwrap(byte[] data, string driverVersion, out ReadOnlySpan<byte> blob)
    {
        blob = default;
        if (data.Length < HeaderSize)
            return false;

        var header = data.AsSpan(0, HeaderSize);
        if (BinaryPrimitives.ReadUInt32LittleEndian(header[MagicOffset..]) != Magic
            || BinaryPrimitives.ReadUInt32LittleEndian(header[VersionOffset..]) != Version)
            return false;

        var length = BinaryPrimitives.ReadInt32LittleEndian(header[LengthOffset..]);
        if (length <= 0 || length != data.Length - HeaderSize)
            return false;

        if (ReadHash(header[DriverVersionOffset..]) != HashOf(driverVersion))
            return false;

        var content = data.AsSpan(HeaderSize, length);
        if (ObjectId.FromBytes(content) != ReadHash(header[HashOffset..]))
            return false;

        blob = content;
        return true;
    }

    private static ObjectId HashOf(string driverVersion) => ObjectId.FromBytes(Encoding.UTF8.GetBytes(driverVersion ?? string.Empty));

    private static void WriteHash(ObjectId hash, Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(destination, hash.Hash1);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], hash.Hash2);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], hash.Hash3);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], hash.Hash4);
    }

    private static ObjectId ReadHash(ReadOnlySpan<byte> source) => new(
        BinaryPrimitives.ReadUInt32LittleEndian(source),
        BinaryPrimitives.ReadUInt32LittleEndian(source[4..]),
        BinaryPrimitives.ReadUInt32LittleEndian(source[8..]),
        BinaryPrimitives.ReadUInt32LittleEndian(source[12..]));
}
