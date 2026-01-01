using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Stride.Shaders.Compilers;

public abstract class ShaderLoaderBase : IExternalShaderLoader
{
    record struct ShaderLoadKey(ShaderMacro[] Macros)
    {
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                foreach (var current in Macros)
                    hashCode = (hashCode * 397) ^ (current.GetHashCode());
                return hashCode;
            }
        }

        public bool Equals(ShaderLoadKey other)
        {
            return Macros.SequenceEqual(other.Macros);
        }
    }

    private Dictionary<string, Dictionary<ShaderLoadKey, SpirvBytecode>> loadedShaders = [];

    public void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, SpirvBytecode buffer)
    {
        if (!loadedShaders.TryGetValue(name, out var loadedShadersByName))
            loadedShaders.Add(name, loadedShadersByName = new());
        loadedShadersByName.Add(new(defines.ToArray()), buffer);
    }

    public bool Exists(string name)
    {
        if (loadedShaders.ContainsKey(name))
            return true;

        return ExternalFileExists(name);
    }

    protected abstract bool ExternalFileExists(string name);
    protected abstract bool LoadExternalFileContent(string name, out string filename, out string code);

    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out SpirvBytecode buffer, out bool isFromCache)
    {
        if (loadedShaders.TryGetValue(name, out var loadedShadersByName)
            && loadedShadersByName.TryGetValue(new(defines.ToArray()), out buffer))
        {
            isFromCache = true;
            return true;
        }

        isFromCache = false;
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

    public bool LoadExternalBuffer(string name, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out SpirvBytecode buffer, out bool isFromCache)
    {
        if (loadedShaders.TryGetValue(name, out var loadedShadersByName)
            && loadedShadersByName.TryGetValue(new(defines.ToArray()), out buffer))
        {
            isFromCache = true;
            return true;
        }

        var filename = $"{code}.sdsl";

        isFromCache = false;
        if (!LoadFromCode(filename, code, defines, out buffer))
        {
            throw new InvalidOperationException($"Shader {name} could not be compiled");
        }

        return true;
    }

    protected virtual bool LoadFromCode(string filename, string code, ReadOnlySpan<ShaderMacro> macros, out SpirvBytecode buffer)
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