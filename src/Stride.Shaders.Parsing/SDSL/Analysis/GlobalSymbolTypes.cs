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
#if NET8_0_OR_GREATER
        return mixins.TryAdd(symbol.Name, symbol);
#else
        if (mixins.ContainsKey(symbol.Name))
            return false;
        else
        {
            Register(symbol);
            return true;
        }
#endif
    }

    public static MixinSymbol Get(string name)
    {
        return mixins[name];
    }
    
    public static bool TryGet(string name, out MixinSymbol symbol)
    {
        return mixins.TryGetValue(name, out symbol);
    }
}