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
        var spirv = new SpirvEmitter(455);
        spirv.Construct(program,entry);
        return spirv;
    }
}