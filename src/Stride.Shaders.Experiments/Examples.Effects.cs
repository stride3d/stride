using System.Diagnostics.CodeAnalysis;
using System.Text;
using Stride.Core.Storage;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Experiments;


public static partial class Examples
{

    public static void CompileBasicEffect()
    {
        var loader = new EffectLoader();
        loader.LoadExternalBuffer("BasicEffect.sdfx", [], out var effectBuffer, out _, out _);
        loader.LoadExternalBuffer("BasicEffect", [], out effectBuffer, out _, out _);

        Spv.Dis(effectBuffer, DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
    }

    public class EffectLoader() : ShaderLoaderBase(new ShaderCache())
    {
        protected override bool ExternalFileExists(string name)
        {
            var filename = $"./assets/SDFX/{name}{(Path.HasExtension(name) ? "" : ".sdsl")}";
            return File.Exists(filename);
        }

        public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
        {
            filename = $"./assets/SDFX/{name}{(Path.HasExtension(name) ? "" : ".sdsl")}";

            var fileData = File.ReadAllBytes(filename);
            hash = ObjectId.FromBytes(fileData);
    
            using var reader = new StreamReader(new MemoryStream(fileData), Encoding.UTF8);
            code = reader.ReadToEnd();
            
            return true;
        }
    }
}