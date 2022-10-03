using Spv.Generator;
using Stride.Shaders.Mixer;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter
{
    public Stream Stream { get; set; }


    public void CreateStreamStructs(ShaderProgram program, EntryPoints entry)
    {
        MainMethod mainMethod = entry switch
        {
            EntryPoints.VSMain => (MainMethod)program.Body.First(x => x is VSMainMethod),
            EntryPoints.PSMain => (MainMethod)program.Body.First(x => x is PSMainMethod),
            EntryPoints.GSMain => (MainMethod)program.Body.First(x => x is GSMainMethod),
            EntryPoints.CSMain => (MainMethod)program.Body.First(x => x is CSMainMethod),
            EntryPoints.DSMain => (MainMethod)program.Body.First(x => x is DSMainMethod),
            EntryPoints.HSMain => (MainMethod)program.Body.First(x => x is HSMainMethod),
            _ => throw new NotImplementedException()
        };
        var structs = program.Symbols.GetAllStructTypes();
        program.Symbols.TryGetType("STREAM", out var streamType);
        //var fields = ((CompositeType)streamType).Fields.Select(x => x.Field)
        var x = 0;


        
        
    }
}