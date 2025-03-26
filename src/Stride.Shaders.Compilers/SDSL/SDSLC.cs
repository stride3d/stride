using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Tools;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Runtime.InteropServices;

namespace Stride.Shaders.Compilers.SDSL;

public record struct SDSLC() : ICompiler
{
    public readonly bool Compile(string code, out byte[] compiled)
    {
        var parsed = SDSLParser.Parse(code);
        if(parsed.AST is ShaderFile sf)
        {
            SymbolTable table = new();
            var shader = sf.Namespaces.First().Declarations.OfType<ShaderClass>().First();
            shader.ProcessSymbol(table);

            if(table.Errors.Count > 0)
                throw new Exception("Some parse errors");
            using var compiler = new CompilerUnit();
            shader.Compile(compiler, table);

            compiler.Context.Buffer.Sort();
            var merged = SpirvBuffer.Merge(compiler.Context.Buffer, compiler.Builder.Buffer);
            var dis = new SpirvDis<SpirvBuffer>(merged, true);
            dis.Disassemble(true);
            compiled = MemoryMarshal.AsBytes(merged.Span).ToArray();
            return true;
        }
        else
        {
            compiled = [];
            return false;
        }
    }
}