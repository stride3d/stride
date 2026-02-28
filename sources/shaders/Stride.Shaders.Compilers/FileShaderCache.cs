using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Compilers;

/// <summary>
/// File-backed <see cref="IShaderCache"/> that persists compiled shader bytecodes
/// to disk via <see cref="IVirtualFileProvider"/>, falling back to in-memory cache for hot lookups.
/// </summary>
public class FileShaderCache(IVirtualFileProvider fileProvider, string basePath = "shader/cache") : IShaderCache
{
    private readonly ShaderCache memoryCache = new();

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

    public void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, ShaderBuffers bytecode, ObjectId? hash)
    {
        memoryCache.RegisterShader(name, defines, bytecode, hash);

        try
        {
            var path = GetCachePath(name, defines);
            var dir = path[..path.LastIndexOf('/')];
            if (!fileProvider.DirectoryExists(dir))
                fileProvider.CreateDirectory(dir);

            using var stream = fileProvider.OpenStream(path, VirtualFileMode.Create, VirtualFileAccess.Write);
            using var writer = new BinaryWriter(stream);
            Serialize(writer, bytecode, hash ?? ObjectId.Empty);
        }
        catch
        {
            // Silently ignore write failures — next run will just recompile
        }
    }

    public bool TryLoadFromCache(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash)
    {
        if (memoryCache.TryLoadFromCache(name, defines, out buffer, out hash))
            return true;

        try
        {
            var path = GetCachePath(name, defines);
            if (!fileProvider.FileExists(path))
            {
                buffer = default;
                hash = default;
                return false;
            }

            using var stream = fileProvider.OpenStream(path, VirtualFileMode.Open, VirtualFileAccess.Read);
            using var reader = new BinaryReader(stream);
            buffer = Deserialize(reader, out hash);

            // Populate in-memory cache for subsequent lookups
            memoryCache.RegisterShader(name, defines, buffer, hash);
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

    private string GetCachePath(string name, ReadOnlySpan<ShaderMacro> defines)
    {
        var sanitized = SanitizeName(name);
        var macrosKey = defines.Length == 0 ? "default" : ComputeMacrosHash(defines);
        return $"{basePath}/{sanitized}/{macrosKey}";
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

    private static string ComputeMacrosHash(ReadOnlySpan<ShaderMacro> defines)
    {
        var builder = new ObjectIdBuilder();
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
        // Header
        writer.Write(buffers.Context.Bound);
        WriteObjectId(writer, hash);

        // Context buffer
        var contextBuffer = buffers.Context.GetBuffer();
        writer.Write(GetTotalWordCount(contextBuffer));
        WriteBuffer(writer, contextBuffer);

        // Main buffer
        WriteBuffer(writer, buffers.Buffer);
    }

    private static ShaderBuffers Deserialize(BinaryReader reader, out ObjectId hash)
    {
        // Header
        var bound = reader.ReadInt32();
        hash = ReadObjectId(reader);

        // Context buffer
        var contextWordCount = reader.ReadInt32();
        var contextWords = new int[contextWordCount];
        for (int i = 0; i < contextWordCount; i++)
            contextWords[i] = reader.ReadInt32();

        // Main buffer — read remaining bytes
        var remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
        var mainWordCount = (int)(remainingBytes / sizeof(int));
        var mainWords = new int[mainWordCount];
        for (int i = 0; i < mainWordCount; i++)
            mainWords[i] = reader.ReadInt32();

        // Reconstruct
        var contextBuffer = new SpirvBuffer(contextWords);
        var mainBuffer = new SpirvBuffer(mainWords);
        var context = new SpirvContext(contextBuffer) { Bound = bound };
        ShaderClass.ProcessNameAndTypes(context);
        return new ShaderBuffers(context, mainBuffer);
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
