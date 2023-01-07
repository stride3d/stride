using Spv;
using Spv.Generator;
using SDSL.Parsing;
using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.AST.Shader.Analysis;
using SDSL.Spirv;
using SDSL.ThreeAddress;

namespace SDSL.Mixer;

public class SimpleMixer
{
    ShaderClassString source;
    public ShaderProgram program;

    public SimpleMixer(string className, ShaderSourceManager manager)
    {
        source = new(manager.GetShaderSource(className));
        program = ShaderMixinParser.ParseShader(source.ShaderSourceCode);
    }
    public ErrorList SemanticChecks<T>() where T : MainMethod
    {
        var errors = program.SemanticChecks<T>();
        
        return errors;
    }
    public Module EmitSpirv(EntryPoints entry)
    {
        var spirv = new SpirvEmitter(Specification.Version);
        spirv.Construct(program,entry);
        return spirv;
    }
}