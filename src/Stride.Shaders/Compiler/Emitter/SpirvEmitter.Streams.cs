using Stride.Shaders.Mixer;
using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter
{    
    public void CreateStreamStructs(ShaderProgram program, EntryPoints entry)
    {
        var name = entry switch {
            EntryPoints.VSMain => 'V',
            EntryPoints.PSMain => 'P',
            EntryPoints.GSMain => 'G',
            EntryPoints.CSMain => 'C',
            EntryPoints.DSMain => 'D',
            EntryPoints.HSMain => 'H',
            _ => throw new NotImplementedException()
        };
        var variables = 
            program.Body
            .Where(x => x is ShaderVariableDeclaration svd && svd.IsStream)
            .Cast<ShaderVariableDeclaration>()
            .Select(x => (x.Name, SpvType: ShaderTypes[x.Type]));
        
        var streams = TypeStruct(false, variables.Select(x => x.SpvType).ToArray());
        Name(streams, name + "S_STREAMS");
        for (int i = 0; i < variables.Count(); i++)
        {
            MemberName(streams,i,variables.ElementAt(i).Name);
        }

        MainMethod mainMethod = entry switch {
            EntryPoints.VSMain => (MainMethod)program.Body.First(x => x is VSMainMethod),
            EntryPoints.PSMain => (MainMethod)program.Body.First(x => x is PSMainMethod),
            EntryPoints.GSMain => (MainMethod)program.Body.First(x => x is GSMainMethod),
            EntryPoints.CSMain => (MainMethod)program.Body.First(x => x is CSMainMethod),
            EntryPoints.DSMain => (MainMethod)program.Body.First(x => x is DSMainMethod),
            EntryPoints.HSMain => (MainMethod)program.Body.First(x => x is HSMainMethod),
            _ => throw new NotImplementedException()
        };
        var likelyOutput = mainMethod.GetStreamValuesAssigned();
        var outVars = 
            variables.Where(x => likelyOutput.Contains(x.Name));
        var outStream = TypeStruct(true, outVars.Select(x => x.SpvType).ToArray());
        Name(outStream, name + "S_STREAMS_OUT");
        for (int i = 0; i < outVars.Count(); i++)
        {
            MemberName(outStream,i,variables.ElementAt(i).Name);
        }
    }
}