using Stride.Shaders.Parsing.SDSL.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public static class GlobalShaderTypes
{
    static Dictionary<string, MixinSymbol> mixins = [];


    public static void Register(MixinSymbol symbol)
    {
        mixins[symbol.Name] = symbol;
    }
    public static MixinSymbol Get(string name)
    {
        return mixins[name];
    }
}