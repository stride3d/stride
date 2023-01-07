using SDSL.Mixer;
using SDSL.Parsing;

namespace SDSL;

public class EffectCompiler
{
    public ShaderLoader Loader {get;set;}

    public EffectCompiler(string path)
    {  
        Loader = new ShaderLoader(path);
    }

}