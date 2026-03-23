using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Stride.Shaders.Compilers;

public abstract class ShaderLoaderBase(IShaderCache fileCache) : IExternalShaderLoader
{
    public IShaderCache Cache => fileCache;

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

        if (!ExternalFileExists(name))
        {
            throw new InvalidOperationException($"Shader {name} could not be found");
        }

        if (!LoadExternalFileContent(name, out var filename, out var code, out hash))
        {
            throw new InvalidOperationException($"Shader {name} could not be loaded");
        }

        if (!LoadFromCode(filename, code, hash, defines, out buffer))
        {
            // If a logger is set, errors are already logged — just return false
            if (Log != null)
                return false;
            throw new InvalidOperationException($"Shader {name} could not be compiled");
        }

        return true;
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
        if (!sdslc.Compile(filename, text, hash, macros, log, out buffer, registerInCache))
        {
            if (log is LoggerResult loggerResult && loggerResult.HasErrors)
                throw new InvalidOperationException(string.Join(Environment.NewLine, loggerResult.Messages.Where(m => m.Type >= LogMessageType.Error).Select(m => m.Text)));
            return false;
        }
        return true;
    }
}
