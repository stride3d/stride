using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Building;
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
            return true;

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

    public bool LoadExternalBuffer(string name, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash, out bool isFromCache)
    {
        isFromCache = Cache.TryLoadFromCache(name, null, defines, out buffer, out hash);
        if (isFromCache)
            return true;

        var filename = $"{code}{(Path.HasExtension(code) ? "" : ".sdsl")}";

        hash = ObjectId.FromBytes(Encoding.UTF8.GetBytes(code));
        if (!LoadFromCode(filename, code, hash, defines, out buffer))
        {
            // If a logger is set, errors are already logged — just return false
            if (Log != null)
                return false;
            throw new InvalidOperationException($"Shader {name} could not be compiled");
        }

        return true;
    }

    protected virtual bool LoadFromCode(string filename, string code, ObjectId hash, ReadOnlySpan<ShaderMacro> macros, out ShaderBuffers buffer)
    {
        var defines = new (string Name, string Definition)[macros.Length];
        for (int i = 0; i < macros.Length; ++i)
            defines[i] = (macros[i].Name, macros[i].Definition);

        var text = MonoGamePreProcessor.Run(code, Path.GetFileName(filename), defines);
        var sdslc = new SDSLC
        {
            ShaderLoader = this,
        };

        // Use provided logger, or a temporary one that throws on errors
        var log = Log ?? new LoggerResult();
        if (!sdslc.Compile(filename, text, hash, macros, log, out buffer))
        {
            if (Log == null && log is LoggerResult loggerResult && loggerResult.HasErrors)
                throw new InvalidOperationException(string.Join(Environment.NewLine, loggerResult.Messages.Where(m => m.Type >= LogMessageType.Error).Select(m => m.Text)));
            return false;
        }
        return true;
    }
}
