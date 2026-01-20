using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Stride.Core.Storage;

namespace Stride.Shaders.Compilers;

public abstract class ShaderLoaderBase(IShaderCache fileCache) : IExternalShaderLoader
{
    public IShaderCache FileCache => fileCache;
    public IShaderCache GenericCache { get; } = new ShaderCache();

    public bool Exists(string name)
    {
        if (fileCache.Exists(name))
            return true;

        return ExternalFileExists(name);
    }

    protected abstract bool ExternalFileExists(string name);
    public abstract bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash);

    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash, out bool isFromCache)
    {
        isFromCache = fileCache.TryLoadFromCache(name, defines, out buffer, out hash);
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
            throw new InvalidOperationException($"Shader {name} could not be compiled");
        }

        return true;
    }

    public bool LoadExternalBuffer(string name, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash, out bool isFromCache)
    {
        isFromCache = fileCache.TryLoadFromCache(name, defines, out buffer, out hash);
        if (isFromCache)
            return true;

        var filename = $"{code}.sdsl";

        hash = ObjectId.FromBytes(Encoding.UTF8.GetBytes(code));
        if (!LoadFromCode(filename, code, hash, defines, out buffer))
        {
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

        return sdslc.Compile(text, hash, macros, out buffer);
    }
}