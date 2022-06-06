using Stride.Shaders.Parsing;

namespace Stride.Shaders.Compiling;

public class ShaderCompiler
{
    public ShaderMixinParser Parser {get;set;} = new();
    public ShaderClassString Shader {get;set;}

    public void Compile(string shader, Dictionary<string,string> macros)
    {
        
    }
}