using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter
{    
    public void CreateStreamStructs(ShaderProgram program)
    {
        var variables = 
            program.Body
            .Where(x => x is ShaderVariableDeclaration svd && svd.IsStream)
            .Cast<ShaderVariableDeclaration>()
            .Select(x => (x.Name, SpvType: ShaderTypes[x.Type]));
        
        var streams = TypeStruct(false, variables.Select(x => x.SpvType).ToArray());
        Name(streams,"VS_STREAMS");
        for (int i = 0; i < variables.Count(); i++)
        {
            MemberName(streams,i,variables.ElementAt(i).Name);
        }
    }
}