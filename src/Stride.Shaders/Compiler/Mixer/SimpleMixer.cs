using Spv.Generator;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.ThreeAddress;

namespace Stride.Shaders.Mixer;

public class SimpleMixer
{
    ShaderClassString source;
    public ShaderProgram program;
    List<Register> il;

    public SimpleMixer(string className, ShaderSourceManager manager)
    {
        source = new(manager.GetShaderSource(className));
        program = ShaderMixinParser.ParseShader(source.ShaderSourceCode);
        il = new();
    }
    public void SemanticChecks()
    {
        var sym = new SymbolTable();
        sym.PushStreamType(program.Body.OfType<ShaderVariableDeclaration>());
        
        foreach(var method in program.Body.OfType<MainMethod>())
        {
            sym.AddScope();
            method.VariableChecking(sym);
            sym.Pop();
        }
    }
    public Module EmitSpirv(EntryPoints entry)
    {
        var spirv = new SpirvEmitter(455);
        spirv.Construct(program,entry);
        return spirv;
    }
}