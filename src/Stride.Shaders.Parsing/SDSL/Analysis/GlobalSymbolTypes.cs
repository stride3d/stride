using Stride.Shaders.Parsing.SDSL.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public static class GlobalShaderTypes
{
    static Dictionary<string, MixinSymbol> mixins = [];


    public static void Register(MixinSymbol symbol)
    {
        mixins.Add(symbol.Name, symbol);
    }

    public static bool TryRegister(MixinSymbol symbol)
    {
        return mixins.TryAdd(symbol.Name, symbol);
    }

    public static MixinSymbol Get(string name)
    {
        return mixins[name];
    }
    
    public static bool TryGet(string name, out MixinSymbol? symbol)
    {
        return mixins.TryGetValue(name, out symbol);
    }
}