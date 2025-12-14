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
using Stride.Shaders.Parsing.SDFX.AST;

namespace Stride.Shaders.Compilers.SDSL;

public record struct SDSLC(IExternalShaderLoader ShaderLoader)
{
    public readonly bool Compile(string code, ReadOnlySpan<ShaderMacro> macros, [MaybeNullWhen(false)] out NewSpirvBuffer lastBuffer)
    {
        var parsed = SDSLParser.Parse(code);
        lastBuffer = null;
        if (parsed.Errors.Count > 0)
        {
            throw new Exception($"Some parse errors:{Environment.NewLine}{string.Join(Environment.NewLine, parsed.Errors)}");
        }
        if(parsed.AST is ShaderFile sf)
        {
            // TODO: support namespace
            var declarations = sf.Namespaces.SelectMany(x => x.Declarations).Concat(sf.RootDeclarations);
            foreach (var declaration in declarations)
            {
                if (declaration is ShaderClass shader)
                {
                    SymbolTable table = new()
                    {
                        ShaderLoader = ShaderLoader,
                        CurrentMacros = [..macros],
                    };
                    var compiler = new CompilerUnit();
                    compiler.Macros.AddRange(macros);
                    shader.Compile(table, compiler);

                    if (table.Errors.Count > 0)
                        throw new Exception("Some parse errors");

                    var merged = compiler.ToBuffer();
#if DEBUG
                    var dis = Spv.Dis(merged);
#endif
                    lastBuffer = merged;

                    ShaderLoader.RegisterShader(shader.Name, macros, merged);
                }
                else if (declaration is ShaderEffect effect)
                {
                    SymbolTable table = new()
                    {
                        ShaderLoader = ShaderLoader,
                        CurrentMacros = [..macros],
                    };
                    var compiler = new CompilerUnit();
                    compiler.Macros.AddRange(macros);
                    effect.Compile(table, compiler);

                    var merged = compiler.ToBuffer();
#if DEBUG
                    var dis = Spv.Dis(merged);
#endif
                    lastBuffer = merged;

                    ShaderLoader.RegisterShader(effect.Name, macros, merged);
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