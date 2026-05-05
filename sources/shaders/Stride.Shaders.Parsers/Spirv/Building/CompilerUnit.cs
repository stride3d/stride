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

    public int? SourceFileId { get; set; }
    public List<ShaderMacro> Macros { get; } = [];

    public CompilerUnit()
    {
        Context = new SpirvContext();
        Builder = new SpirvBuilder();
        Arguments = [];
    }

    public CompilerUnit(SpirvContext context, SpirvBuilder builder)
    {
        Context = context;
        Builder = builder;
        Arguments = [];
    }

    public void Deconstruct(out SpirvBuilder builder, out SpirvContext context)
    {
        builder = Builder;
        context = Context;
    }

    public SpirvBuffer ToBuffer()
    {
        Context.Sort();
        return SpirvBuffer.Merge(Context.GetBuffer(), Builder.GetBuffer());
    }

    public ShaderBuffers ToShaderBuffers()
    {
        Context.Sort();
        return new(Context, Builder.GetBuffer());
    }

    public override string ToString()
    {
        return ToBuffer().GetDebuggerDisplay();
    }
}
