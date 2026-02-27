using Stride.Shaders.Compilers;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Tools;
using System.Text;
using Stride.Core.Storage;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

class ShaderLoader(params string[] searchPaths) : ShaderLoaderBase(new TestShaderCache())
{
    protected override bool ExternalFileExists(string name)
    {
        foreach (var basePath in searchPaths)
        {
            if (File.Exists($"{basePath}/{name}.sdsl"))
                return true;
        }
        return false;
    }

    public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
    {
        foreach (var basePath in searchPaths)
        {
            filename = $"{basePath}/{name}.sdsl";
            if (!File.Exists(filename))
                continue;

            var fileData = File.ReadAllBytes(filename);
            hash = ObjectId.FromBytes(fileData);

            // Note: we can't use Encoding.UTF8.GetString directly because there might be the UTF8 BOM at the beginning of the file
            using var reader = new StreamReader(new MemoryStream(fileData), Encoding.UTF8);
            code = reader.ReadToEnd();

            return true;
        }

        filename = "";
        code = "";
        hash = default;
        return false;
    }

    protected override bool LoadFromCode(string filename, string code, ObjectId hash, ReadOnlySpan<ShaderMacro> macros, out ShaderBuffers buffer)
    {
        var result = base.LoadFromCode(filename, code, hash, macros, out buffer);
        if (result)
        {
            Console.WriteLine($"Loading shader {filename}");
            Spv.Dis(buffer, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        }
        return result;
    }

    class TestShaderCache : ShaderCache
    {
        public override void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, ShaderBuffers bytecode, ObjectId? hash = null)
        {
            base.RegisterShader(name, defines, bytecode, hash);

            Console.WriteLine($"Registering shader {name}");
            Spv.Dis(bytecode, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
        }
    }
}
