using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Tools;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Processing;

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

            // temp hack to add entry point (last function)
            var context = compiler.Context;
            if (context.Module.Functions.Count > 0)
            {
                var entryPoint = context.Module.Functions[^1];
                context.Buffer.AddOpCapability(Spv.Specification.Capability.Shader);
                context.Buffer.AddOpMemoryModel(Spv.Specification.AddressingModel.Logical, Spv.Specification.MemoryModel.GLSL450);
                context.SetEntryPoint(Spv.Specification.ExecutionModel.Vertex, entryPoint.Id, entryPoint.Name, []);

                new StreamAnalyzer().Process(table, compiler, entryPoint);
            }

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