using System.Text;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Spirv.Building;


public abstract class CompilerArgument;


public class CompilerUnit
{
    public SpirvContext Context { get; }
    public SpirvBuilder Builder { get; }
    public List<CompilerArgument> Arguments { get; }

    public CompilerUnit()
    {
        Context = new SpirvContext();
        Builder = new SpirvBuilder();
        Arguments = [];
    }

    public void Deconstruct(out SpirvBuilder builder, out SpirvContext context)
    {
        builder = Builder;
        context = Context;
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public NewSpirvBuffer ToBuffer()
    {
        Context.Sort();
        return NewSpirvBuffer.Merge(Context.GetBuffer(), Builder.GetBuffer());
    }
    // public override string ToString()
    // {
    //     var builder = new StringBuilder();
    //     builder
    //         .AppendLine("Context : ")
    //         .AppendLine(Spv.Dis(Context.GetBuffer()))
    //         .AppendLine("Functions : ")
    //         .AppendLine(Spv.Dis(Builder.GetBuffer()));
    //     return builder.ToString();
    // }
#pragma warning restore CS0618 // Type or member is obsolete
}