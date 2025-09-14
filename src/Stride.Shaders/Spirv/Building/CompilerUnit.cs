using System.Text;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Spirv.Building;


public abstract class CompilerArgument;


public class CompilerUnit
{
    public SpirvModule Module { get; }
    public SpirvContext Context { get; }
    public SpirvBuilder Builder { get; }
    public List<CompilerArgument> Arguments { get; }

    public CompilerUnit()
    {
        Module = new SpirvModule();
        Context = new SpirvContext(Module);
        Builder = new SpirvBuilder();
        Arguments = [];
    }

    public void Deconstruct(out SpirvBuilder builder, out SpirvContext context, out SpirvModule module)
    {
        builder = Builder;
        context = Context;
        module = Module;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder
            .AppendLine("Context : ")
            .AppendLine(Spv.Dis(Context.Buffer))
            .AppendLine("Functions : ")
            .AppendLine(Spv.Dis(Builder.Buffer));
        return builder.ToString();
    }
}