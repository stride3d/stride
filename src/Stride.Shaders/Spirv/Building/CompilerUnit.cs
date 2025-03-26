using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Spirv.Building;


public abstract class CompilerArgument;


public class CompilerUnit : IDisposable
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

    public void Dispose()
    {
        Builder.Dispose();
        Context.Dispose();
    }
}