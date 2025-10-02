using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Stride.Shaders.Compilers.SDSL;

public record struct SDSLC(IExternalShaderLoader ShaderLoader)
{
    public readonly bool Compile(string code, [MaybeNullWhen(false)] out NewSpirvBuffer lastBuffer)
    {
        var parsed = SDSLParser.Parse(code);
        if(parsed.AST is ShaderFile sf)
        {
            lastBuffer = null;
            foreach (var declaration in sf.Namespaces.First().Declarations)
            {
                if (declaration is ShaderClass shader)
                {
                    SymbolTable table = new()
                    {
                        ShaderLoader = ShaderLoader
                    };
                    var compiler = new CompilerUnit();
                    shader.Compile(compiler, table);

                    if (table.Errors.Count > 0)
                        throw new Exception("Some parse errors");

                    var merged = compiler.ToBuffer();
                    var dis = Spv.Dis(merged, true);
                    lastBuffer = merged;

                    ShaderLoader.RegisterShader(shader.Name, merged);
                }
                else
                {
                    throw new NotImplementedException($"Compiling declaration [{declaration.GetType()}] is not implemented");
                }
            }

            return lastBuffer != null;
        }
        else
        {
            lastBuffer = null;
            return false;
        }
    }
}