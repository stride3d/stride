using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Stride.Shaders.Compilers;

public abstract class ShaderLoaderBase(IShaderCache fileCache) : IExternalShaderLoader
{
    public IShaderCache Cache => fileCache;
    public GenericShaderCache GenericCache { get; } = new();
    public bool SuppressSourceHash { get; set; }

    /// <summary>
    /// Ensures only one thread compiles a given shader at a time. Other threads wait for the result.
    /// </summary>
    private readonly ConcurrentDictionary<(string Name, int MacrosHash), Lazy<(ShaderBuffers Buffer, ObjectId Hash)>> compilingShaders = new();

    /// <summary>
    /// Optional logger for compilation errors. If not set, errors are thrown as exceptions.
    /// </summary>
    public ILogger? Log { get; set; }

    public bool Exists(string name)
    {
        if (Cache.Exists(name))
            return true;

        return ExternalFileExists(name);
    }

    protected abstract bool ExternalFileExists(string name);
    public abstract bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash);

    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash, out bool isFromCache)
    {
        isFromCache = Cache.TryLoadFromCache(name, null, defines, out buffer, out hash);
        if (isFromCache)
        {
            if (ValidateCachedHashes(buffer))
                return true;
            // A dependency changed — invalidate and recompile
            isFromCache = false;
        }

        // Coordinate parallel compilations: only one thread compiles a given (name, macros) pair.
        var macrosHash = ComputeMacrosHash(defines);
        var macrosArray = defines.ToArray();
        var key = (name, macrosHash);

        var lazy = compilingShaders.GetOrAdd(key, _ => new Lazy<(ShaderBuffers, ObjectId)>(() =>
        {
            // Double-check cache (another thread may have finished between our check and this factory)
            if (Cache.TryLoadFromCache(name, null, macrosArray, out var buf, out var h) && ValidateCachedHashes(buf))
                return (buf, h);

            if (!ExternalFileExists(name))
                throw new InvalidOperationException($"Shader {name} could not be found");

            if (!LoadExternalFileContent(name, out var filename, out var code, out h))
                throw new InvalidOperationException($"Shader {name} could not be loaded");

            if (!LoadFromCode(filename, code, h, macrosArray, out buf))
                throw new InvalidOperationException($"Shader {name} could not be compiled");

            return (buf, h);
        }, LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var result = lazy.Value;
            buffer = result.Buffer;
            hash = result.Hash;
            isFromCache = false;
            return true;
        }
        catch
        {
            compilingShaders.TryRemove(key, out _);
            throw;
        }
        finally
        {
            compilingShaders.TryRemove(key, out _);
        }
    }

    public bool LoadExternalBuffer(string name, string? filename, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash, out bool isFromCache)
    {
        isFromCache = Cache.TryLoadFromCache(name, null, defines, out buffer, out hash);
        if (isFromCache)
            return true;

        hash = ObjectId.FromBytes(Encoding.UTF8.GetBytes(code));
        // Don't auto-register in SDSLC — the caller (InstantiateMemberNames) registers under the cache key
        if (!LoadFromCode(filename, code, hash, defines, out buffer, registerInCache: false))
        {
            // If a logger is set, errors are already logged — just return false
            if (Log != null)
                return false;
            throw new InvalidOperationException($"Shader {name} could not be compiled");
        }

        return true;
    }

    /// <summary>
    /// Validates all OpSourceHashSDSL entries in a cached buffer against current file content.
    /// Returns false if any dependency has changed.
    /// </summary>
    private bool ValidateCachedHashes(ShaderBuffers buffer)
    {
        foreach (var i in buffer.Context)
        {
            if (i.Op == Specification.Op.OpSourceHashSDSL && (OpSourceHashSDSL)i is { } sourceHash)
            {
                var cachedHash = new ObjectId((uint)sourceHash.Hash1, (uint)sourceHash.Hash2, (uint)sourceHash.Hash3, (uint)sourceHash.Hash4);

                // Resolve filename from OpString
                string? filename = null;
                foreach (var s in buffer.Context)
                {
                    if (s.Op == Specification.Op.OpString && ((OpString)s).ResultId == sourceHash.File)
                    {
                        filename = ((OpString)s).Value;
                        break;
                    }
                }
                if (filename == null)
                    continue;

                // Extract shader name from filename (strip path and extension)
                var shaderName = Path.GetFileNameWithoutExtension(filename);
                if (LoadExternalFileContent(shaderName, out _, out _, out var currentHash))
                {
                    if (cachedHash != currentHash)
                        return false;
                }
                // If file not found, it might be an internal shader — skip validation
            }
        }
        return true;
    }

    private static int ComputeMacrosHash(ReadOnlySpan<ShaderMacro> macros)
    {
        unchecked
        {
            int hash = 0;
            foreach (var m in macros)
                hash = hash * 397 ^ m.GetHashCode();
            return hash;
        }
    }

    protected virtual bool LoadFromCode(string? filename, string code, ObjectId hash, ReadOnlySpan<ShaderMacro> macros, out ShaderBuffers buffer, bool registerInCache = true)
    {
        var defines = new (string Name, string Definition)[macros.Length];
        for (int i = 0; i < macros.Length; ++i)
            defines[i] = (macros[i].Name, macros[i].Definition);

        var text = MonoGamePreProcessor.Run(code, filename, defines);
        var sdslc = new SDSLC
        {
            ShaderLoader = this,
        };

        // Use provided logger, or a temporary one that throws on errors
        var log = Log ?? new LoggerResult();
        var emitSourceHash = !SuppressSourceHash;
        SuppressSourceHash = false; // Reset after use
        if (!sdslc.Compile(filename, text, hash, macros, log, out buffer, new() { RegisterInCache = registerInCache, EmitSourceHash = emitSourceHash, OriginalCode = code }))
        {
            if (log is LoggerResult loggerResult && loggerResult.HasErrors)
                throw new InvalidOperationException(string.Join(Environment.NewLine, loggerResult.Messages.Where(m => m.Type >= LogMessageType.Error).Select(m => m.ToString())));
            return false;
        }
        return true;
    }
}
