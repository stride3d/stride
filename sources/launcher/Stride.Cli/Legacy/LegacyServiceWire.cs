// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Formats.Nrbf;
using ServiceWire;

// System.Formats.Nrbf's reader API is still marked experimental; we only use its stable read surface.
#pragma warning disable SYSLIB5005

namespace Stride.Cli.Legacy;

// Stride 4.1 shipped ServiceWire 5.3.4, whose default serializer is BinaryFormatter, and its Commands server
// serializes the ServiceWire handshake payload (ServiceSyncInfo — the remote method table) that way. Later
// ServiceWire (4.2+) switched to a JSON serializer, which the stock client already speaks.
//
// BinaryFormatter is gone from modern .NET, so we can't round-trip that payload with the stock serializer.
// It's the only serializer call on the client's GenerateShaderKeys path, though: the method's string arguments
// and byte[] result travel as ServiceWire primitive type-codes, never through the serializer. So we only need
// to read that one payload, which we do with the safe NRBF reader and rebuild ServiceSyncInfo by hand.
internal sealed class LegacyBinaryFormatterSerializer : ISerializer
{
    public T Deserialize<T>(byte[] bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return default!;
        if (typeof(T) == typeof(ServiceSyncInfo))
            return (T)(object)ReadServiceSyncInfo(bytes);
        throw new NotSupportedException($"Legacy (pre-4.2) ServiceWire deserialize of {typeof(T)} is not supported.");
    }

    public object Deserialize(byte[] bytes, string typeConfigName)
        => throw new NotSupportedException("Legacy (pre-4.2) ServiceWire complex-type deserialize is not supported.");

    public byte[] Serialize<T>(T obj)
        => throw new NotSupportedException("Legacy (pre-4.2) ServiceWire complex-type serialize is not supported.");

    public byte[] Serialize(object obj, string typeConfigName)
        => throw new NotSupportedException("Legacy (pre-4.2) ServiceWire complex-type serialize is not supported.");

    private static ServiceSyncInfo ReadServiceSyncInfo(byte[] bytes)
    {
        var root = (ClassRecord)NrbfDecoder.Decode(new MemoryStream(bytes));

        var methodRecords = ((SZArrayRecord<SerializationRecord>)root.GetArrayRecord(Member(root, "MethodInfos"))).GetArray();
        var methods = new MethodSyncInfo[methodRecords.Length];
        for (var i = 0; i < methodRecords.Length; i++)
        {
            var method = (ClassRecord)methodRecords[i]!;
            methods[i] = new MethodSyncInfo
            {
                MethodIdent = method.GetInt32(Member(method, "MethodIdent")),
                MethodName = method.GetString(Member(method, "MethodName")),
                MethodReturnType = method.GetString(Member(method, "MethodReturnType")),
                ParameterTypes = ((SZArrayRecord<string>)method.GetArrayRecord(Member(method, "ParameterTypes"))).GetArray()!,
            };
        }

        return new ServiceSyncInfo
        {
            ServiceKeyIndex = root.GetInt32(Member(root, "ServiceKeyIndex")),
            UseCompression = root.GetBoolean(Member(root, "UseCompression")),
            CompressionThreshold = root.GetInt32(Member(root, "CompressionThreshold")),
            MethodInfos = methods,
        };
    }

    // BinaryFormatter serializes auto-property backing fields ("<Name>k__BackingField"), so match by property name.
    private static string Member(ClassRecord record, string propertyName)
        => record.MemberNames.FirstOrDefault(name => name == propertyName || name.Contains($"<{propertyName}>"))
           ?? throw new InvalidOperationException($"ServiceWire handshake payload is missing member '{propertyName}'.");
}

// Stride 4.1's ServiceWire (5.3.4) did not compress the wire; pair it with the BinaryFormatter serializer above.
internal sealed class LegacyDoNothingCompressor : ICompressor
{
    public byte[] Compress(byte[] data) => data;

    public byte[] DeCompress(byte[] compressedBytes) => compressedBytes;
}
