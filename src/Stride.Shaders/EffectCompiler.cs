using Stride.Shaders.Mixer;
using Stride.Shaders.Parsing;

namespace Stride.Shaders;

public class EffectCompiler
{
    public ShaderLoader Loader {get;set;}

    public EffectCompiler(string path)
    {  
        Loader = new ShaderLoader(path);
    }

}