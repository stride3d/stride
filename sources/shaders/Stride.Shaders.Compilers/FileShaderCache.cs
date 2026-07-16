using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Compilers;

/// <summary>
/// File-backed <see cref="IShaderCache"/> that persists compiled shader bytecodes
/// to disk via <see cref="IVirtualFileProvider"/>, falling back to in-memory cache for hot lookups.
/// </summary>
public class FileShaderCache(IVirtualFileProvider fileProvider, string basePath = "shaders") : IShaderCache
{
    /// <summary>
    /// Optional importer for resolving struct types from imported shaders during deserialization.
    /// Set after construction to break the circular dependency with the shader loader.
    /// </summary>
    internal IShaderImporter? ShaderImporter { get; set; }

    private readonly object lockObject = new();
    private readonly ShaderCache memoryCache = new();

    // Cache files are stamped in the SPIR-V header: Generator identifies our shader cache, Schema is the
    // format version. A mismatch (including old 0/0 headers) is treated as stale and forces a recompile,
    // which overwrites the file in place. Bump CacheFormatVersion on any incompatible .spv cache change.
    private const int ShaderCacheGeneratorId = 0x5344534C; // 'SDSL'
    private const int CacheFormatVersion = 1;

    public bool Exists(string name)
    {
        if (memoryCache.Exists(name))
            return true;

        try
        {
            var dir = $"{basePath}/{SanitizeName(name)}";
            return fileProvider.DirectoryExists(dir);
        }
        catch
        {
            return false;
        }
    }

    public void RegisterShader(string name, string? generics, ReadOnlySpan<ShaderMacro> defines, ShaderBuffers bytecode, ObjectId? hash)
    {
        memoryCache.RegisterShader(name, generics, defines, bytecode, hash);

        try
        {
            var path = GetCachePath(name, generics, defines);
            var dir = path[..path.LastIndexOf('/')];

            lock (lockObject)
            {
                if (!fileProvider.DirectoryExists(dir))
                    fileProvider.CreateDirectory(dir);

                using var stream = fileProvider.OpenStream(path, VirtualFileMode.Create, VirtualFileAccess.Write);
                using var writer = new BinaryWriter(stream);
                Serialize(writer, bytecode, hash ?? ObjectId.Empty);
            }
        }
        catch
        {
            // Silently ignore write failures — next run will just recompile
        }
    }

    public bool TryLoadFromCache(string name, string? generics, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash)
    {
        if (memoryCache.TryLoadFromCache(name, generics, defines, out buffer, out hash))
            return true;

        try
        {
            var path = GetCachePath(name, generics, defines);
            lock (lockObject)
            {
                if (!fileProvider.FileExists(path))
                {
                    buffer = default;
                    hash = default;
                    return false;
                }

                using var stream = fileProvider.OpenStream(path, VirtualFileMode.Open, VirtualFileAccess.Read);
                using var reader = new BinaryReader(stream);
                if (!TryDeserialize(reader, out buffer, out hash))
                {
                    // Stale/incompatible cache file (old or missing version stamp): treat it like a
                    // cache miss so the caller recompiles and overwrites it in place.
                    buffer = default;
                    hash = default;
                    return false;
                }
            }

            // Populate in-memory cache for subsequent lookups
            memoryCache.RegisterShader(name, generics, defines, buffer, hash);
            return true;
        }
        catch
        {
            // Corrupt or incompatible cache — fall back to recompilation
            buffer = default;
            hash = default;
            return false;
        }
    }

    private string GetCachePath(string name, string? generics, ReadOnlySpan<ShaderMacro> defines)
    {
        var sanitized = SanitizeName(name);
        var macrosKey = defines.Length == 0 ? "default" : ComputeCacheFilename(generics, defines);
        return $"{basePath}/{sanitized}_{macrosKey}.spv";
    }

    private static string SanitizeName(string name)
    {
        Span<char> result = stackalloc char[name.Length];
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            result[i] = c switch
            {
                '<' or '>' or '%' or '"' or '|' or '*' or '?' or ':' => '_',
                _ => c,
            };
        }
        return new string(result);
    }

    private static string ComputeCacheFilename(string? generics, ReadOnlySpan<ShaderMacro> defines)
    {
        var builder = new ObjectIdBuilder();
        if (generics != null)
            builder.Write(generics);
        for (int i = 0; i < defines.Length; i++)
        {
            var nameBytes = Encoding.UTF8.GetBytes(defines[i].Name ?? string.Empty);
            var defBytes = Encoding.UTF8.GetBytes(defines[i].Definition ?? string.Empty);
            builder.Write(nameBytes, 0, nameBytes.Length);
            builder.Write(defBytes, 0, defBytes.Length);
        }
        return builder.ComputeHash().ToString();
    }

    private static void Serialize(BinaryWriter writer, ShaderBuffers buffers, ObjectId hash)
    {
        var header = new SpirvHeader("1.4", generator: ShaderCacheGeneratorId, bound: 1, schema: CacheFormatVersion);
        var bytecode = SpirvBytecode.CreateBytecodeFromBuffers(header, computeBounds: true, buffers.Context.GetBuffer(), buffers.Buffer);
        writer.Write(bytecode);
    }

    /// <summary>
    /// Reads a cached buffer. Returns false (a plain cache miss) when the file predates the current
    /// cache format — an old or absent version stamp is expected, not corruption. Genuinely malformed
    /// content still throws, so callers can distinguish it.
    /// </summary>
    private bool TryDeserialize(BinaryReader reader, [MaybeNullWhen(false)] out ShaderBuffers result, out ObjectId hash)
    {
        result = default;
        hash = default;

        var buffer = new byte[reader.BaseStream.Length];
        reader.ReadExactly(buffer);
        var span = buffer.AsSpan().Cast<byte, int>();

        if (span.Length < SpirvHeader.IntSpanSize)
            return false;

        var header = SpirvHeader.Read(span);
        if (header.Generator != ShaderCacheGeneratorId || header.Schema != CacheFormatVersion)
            return false;

        result = ShaderBuffers.CreateFromSpan(span);

        // Fetch hash from OpSourceHashSDSL
        foreach (var i in result.Context)
        {
            if (i.Op == Spirv.Specification.Op.OpSourceHashSDSL && (OpSourceHashSDSL)i is { } sourceHash)
            {
                hash = new ObjectId((uint)sourceHash.Hash1, (uint)sourceHash.Hash2, (uint)sourceHash.Hash3, (uint)sourceHash.Hash4);
                break;
            }
        }

        ShaderClass.ProcessNameAndTypes(result.Context, ShaderImporter);

        return true;
    }

    private static int GetTotalWordCount(SpirvBuffer buffer)
    {
        int count = 0;
        foreach (var i in buffer)
            count += i.Data.Memory.Length;
        return count;
    }

    private static void WriteBuffer(BinaryWriter writer, SpirvBuffer buffer)
    {
        foreach (var i in buffer)
        {
            var span = i.Data.Memory.Span;
            for (int j = 0; j < span.Length; j++)
                writer.Write(span[j]);
        }
    }

    private static void WriteObjectId(BinaryWriter writer, ObjectId id)
    {
        var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<ObjectId>(in id));
        writer.Write(bytes);
    }

    private static ObjectId ReadObjectId(BinaryReader reader)
    {
        Span<byte> bytes = stackalloc byte[ObjectId.HashSize];
        reader.Read(bytes);
        return Unsafe.ReadUnaligned<ObjectId>(ref MemoryMarshal.GetReference(bytes));
    }
}
