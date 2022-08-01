using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Spirv;

namespace Stride.Shaders.Mixer;

public class SimpleMixer
{
    ShaderClassString source;
    ShaderProgram program;
    List<Register> il;

    public SimpleMixer(string className, ShaderSourceManager manager)
    {
        source = new(manager.GetShaderSource(className));
        program = ShaderMixinParser.ParseShader(source.ShaderSourceCode);
        il = new();
    }
    public void EmitSpirv(EntryPoints entry)
    {
        var spirv = new SpirvEmitter(455);
        spirv.Construct(program,entry);
    }
}