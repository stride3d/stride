using Spv.Generator;
using Stride.Shaders.Mixer;
using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter
{
    public Stream Stream { get; set; }
    public StreamIn StreamIn { get; set; }
    public StreamOut StreamOut { get; set; }


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

        var variables =
            program.Body
            .Where(x => x is ShaderVariableDeclaration svd && svd.IsStream)
            .Cast<ShaderVariableDeclaration>()
            .Select(x => (x.Name, SpvType: AsSpvType(x.Type)))
            .ToList();

        var likelyInputs = mainMethod.GetStreamValuesUsed();
        IEnumerable<(string,Instruction)> inVars = variables.Where(x => likelyInputs.Contains(x.Name)) as IEnumerable<(string,Instruction)>;

        var likelyOutputs = mainMethod.GetStreamValuesAssigned();
        IEnumerable<(string,Instruction)> outVars = variables.Where(x => likelyOutputs.Contains(x.Name)) as IEnumerable<(string,Instruction)>;

        Stream = new Stream(entry, this, variables as IEnumerable<(string,Instruction)>);
        StreamIn = new(entry, this, inVars);
        StreamOut = new StreamOut(entry, this, outVars);
        
    }
}