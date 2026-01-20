using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Stride.Shaders.Compilers;

public abstract class ShaderLoaderBase(IShaderCache cache) : IExternalShaderLoader
{
    public IShaderCache Cache => cache;

    public bool Exists(string name)
    {
        if (cache.Exists(name))
            return true;

        return ExternalFileExists(name);
    }

    protected abstract bool ExternalFileExists(string name);
    protected abstract bool LoadExternalFileContent(string name, out string filename, out string code);

    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out bool isFromCache)
    {
        isFromCache = cache.TryLoadFromCache(name, defines, out buffer);
        if (isFromCache)
            return true;

        if (!ExternalFileExists(name))
        {
            throw new InvalidOperationException($"Shader {name} could not be found");
        }

        if (!LoadExternalFileContent(name, out var filename, out var code))
        {
            throw new InvalidOperationException($"Shader {name} could not be loaded");
        }

        if (!LoadFromCode(filename, code, defines, out buffer))
        {
            throw new InvalidOperationException($"Shader {name} could not be compiled");
        }

        return true;
    }

    public bool LoadExternalBuffer(string name, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out bool isFromCache)
    {
        isFromCache = cache.TryLoadFromCache(name, defines, out buffer);
        if (isFromCache)
            return true;

        var filename = $"{code}.sdsl";

        if (!LoadFromCode(filename, code, defines, out buffer))
        {
            throw new InvalidOperationException($"Shader {name} could not be compiled");
        }

        return true;
    }

    protected virtual bool LoadFromCode(string filename, string code, ReadOnlySpan<ShaderMacro> macros, out ShaderBuffers buffer)
    {
        var defines = new (string Name, string Definition)[macros.Length];
        for (int i = 0; i < macros.Length; ++i)
            defines[i] = (macros[i].Name, macros[i].Definition);

        var text = MonoGamePreProcessor.Run(code, Path.GetFileName(filename), defines);
        var sdslc = new SDSLC
        {
            ShaderLoader = this,
        };

        return sdslc.Compile(text, macros, out buffer);
    }
}